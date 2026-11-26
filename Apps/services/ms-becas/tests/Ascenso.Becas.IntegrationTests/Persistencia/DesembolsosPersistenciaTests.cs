using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class DesembolsosPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public DesembolsosPersistenciaTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PersisteCalendarioYCuotasConCodigosLiterales()
    {
        await using var contexto = _fixture.CrearContexto();
        var calendario = CrearCalendario();
        contexto.CalendariosDesembolso.Add(calendario);
        await contexto.SaveChangesAsync();

        var estado = await contexto.Database.SqlQueryRaw<string>(
            "SELECT estado AS \"Value\" FROM becas.desembolso WHERE calendario_desembolso_id = {0}",
            calendario.Id).Distinct().SingleAsync();
        Assert.Equal("PROGRAMADO", estado);
        Assert.Equal(2, await contexto.Desembolsos.CountAsync(item => item.CalendarioDesembolsoId == calendario.Id));
    }

    [Theory]
    [InlineData("estado = 'INVALIDO'", "ck_desembolso_estado")]
    [InlineData("monto_programado = -1", "ck_desembolso_montos")]
    [InlineData("destinatario = 'INVALIDO'", "ck_desembolso_destinatario")]
    public async Task PostgreSqlRechazaCuotaInvalida(string asignacion, string restriccion)
    {
        await using var contexto = _fixture.CrearContexto();
        var calendario = CrearCalendario();
        contexto.CalendariosDesembolso.Add(calendario);
        await contexto.SaveChangesAsync();

        var cuotaId = calendario.Desembolsos.First().Id;
        var sql = asignacion switch
        {
            "estado = 'INVALIDO'" => "UPDATE becas.desembolso SET estado = 'INVALIDO' WHERE id = {0}",
            "monto_programado = -1" => "UPDATE becas.desembolso SET monto_programado = -1 WHERE id = {0}",
            "destinatario = 'INVALIDO'" => "UPDATE becas.desembolso SET destinatario = 'INVALIDO' WHERE id = {0}",
            _ => throw new ArgumentOutOfRangeException(nameof(asignacion))
        };
        var error = await Assert.ThrowsAsync<PostgresException>(() => contexto.Database.ExecuteSqlRawAsync(sql, cuotaId));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal(restriccion, error.ConstraintName);
    }

    [Fact]
    public async Task PostgreSqlRechazaOtraCuotaVigenteConElMismoNumero()
    {
        await using var contexto = _fixture.CrearContexto();
        var calendario = CrearCalendario();
        contexto.CalendariosDesembolso.Add(calendario);
        await contexto.SaveChangesAsync();

        var original = calendario.Desembolsos.First();
        var segunda = calendario.Desembolsos.Last();
        var error = await Assert.ThrowsAsync<PostgresException>(() => contexto.Database.ExecuteSqlRawAsync(
            "UPDATE becas.desembolso SET numero_cuota = {0} WHERE id = {1}", original.NumeroCuota, segunda.Id));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
    }

    [Fact]
    public async Task PostgreSqlRechazaInsercionConTechoNegativo()
    {
        await using var contexto = _fixture.CrearContexto();
        var calendario = CrearCalendario();
        contexto.CalendariosDesembolso.Add(calendario);
        await contexto.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() => contexto.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO becas.calendario_desembolso
            SELECT (jsonb_populate_record(NULL::becas.calendario_desembolso,
                to_jsonb(t) || jsonb_build_object(
                    'id', gen_random_uuid(),
                    'beca_id', gen_random_uuid(),
                    'monto_maximo_aprobado_periodo', -1))).*
            FROM becas.calendario_desembolso t WHERE id = {0}
            """, calendario.Id));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("ck_calendario_montos", error.ConstraintName);
    }

    [Fact]
    public async Task ConservaLaCadenaDeReintentosConUnaSolaFilaVigente()
    {
        await using var contexto = _fixture.CrearContexto();
        var calendario = CrearCalendario();
        contexto.CalendariosDesembolso.Add(calendario);
        await contexto.SaveChangesAsync();

        var contextoOperacion = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var anterior = calendario.ObtenerCuotaVigente(1);
        calendario.RegistrarEjecucion(1, 50m, new DateOnly(2026, 9, 1),
            DestinatarioPago.Becario, Guid.NewGuid(), "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==",
            MetodoPago.TransferenciaBancaria, "REF-1", contextoOperacion);
        var incidencia = calendario.ReportarIncidencia(1, EstadoDesembolso.Fallido,
            "Cuenta rechazada", "Banco rechazó el abono", contextoOperacion);
        calendario.ResolverIncidencia(1, incidencia.Id, contextoOperacion);
        var nuevo = calendario.Reintentar(1, contextoOperacion);
        await contexto.SaveChangesAsync();

        Assert.Equal(anterior.Id, nuevo.DesembolsoOrigenReintentoId);
        Assert.Equal(2, await contexto.Desembolsos.CountAsync(item => item.CalendarioDesembolsoId == calendario.Id && item.NumeroCuota == 1));
        Assert.Equal(1, await contexto.Desembolsos.CountAsync(item => item.CalendarioDesembolsoId == calendario.Id && item.NumeroCuota == 1 && item.EsVigente));
    }

    [Fact]
    public async Task ConservaEjecucionConfirmadaYOtpAlRecargar()
    {
        await using var contexto = _fixture.CrearContexto();
        var calendario = CrearCalendario();
        contexto.CalendariosDesembolso.Add(calendario);
        await contexto.SaveChangesAsync();

        var operacion = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cuentaId = Guid.NewGuid();
        var otpId = Guid.NewGuid();
        calendario.RegistrarEjecucion(1, 50m, new DateOnly(2026, 9, 1),
            DestinatarioPago.Becario, cuentaId,
            "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==",
            MetodoPago.TransferenciaBancaria, "REF-OTORGADA", operacion);
        calendario.Confirmar(1, otpId, operacion);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var cuota = await contexto.Desembolsos.SingleAsync(item =>
            item.CalendarioDesembolsoId == calendario.Id && item.NumeroCuota == 1);
        Assert.Equal(EstadoDesembolso.PagadoConfirmado, cuota.Estado);
        Assert.Equal(50m, cuota.MontoEjecutado);
        Assert.Equal(cuentaId, cuota.DatosPagoBecarioId);
        Assert.Equal(otpId, cuota.AutorizacionOtpConfirmacionId);
        Assert.Equal(operacion.UsuarioAutorId, cuota.ConfirmadoPorUsuarioId);
        Assert.Equal("REF-OTORGADA", cuota.ReferenciaBancaria);
    }

    [Fact]
    public async Task ConservaVinculosDeSuspensionYPausaAlRecargar()
    {
        await using var contexto = _fixture.CrearContexto();
        var calendario = CrearCalendario();
        contexto.CalendariosDesembolso.Add(calendario);
        await contexto.SaveChangesAsync();

        var operacion = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var suspensionId = Guid.NewGuid();
        var pausaId = Guid.NewGuid();
        calendario.SuspenderCuota(1, EstadoBeca.Suspendida, suspensionId, pausaId, operacion);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var cuota = await contexto.Desembolsos.SingleAsync(item =>
            item.CalendarioDesembolsoId == calendario.Id && item.NumeroCuota == 1);
        Assert.Equal(EstadoDesembolso.Suspendido, cuota.Estado);
        Assert.Equal(suspensionId, cuota.SuspensionDesembolsoId);
        Assert.Equal(pausaId, cuota.PausaAplazamientoId);
    }

    private static CalendarioDesembolso CrearCalendario()
    {
        var fecha = new DateOnly(2026, 9, 1);
        return CalendarioDesembolso.Programar(Guid.NewGuid(), Guid.NewGuid(),
            FrecuenciaDesembolso.Mensual, DestinatarioPago.Becario, 100m,
            fecha, fecha.AddMonths(1),
            [new CuotaProgramada(1, fecha, 50m), new CuotaProgramada(2, fecha.AddMonths(1), 50m)],
            new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}
