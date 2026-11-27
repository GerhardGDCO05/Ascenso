using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Renovacion.Agregados;
using Ascenso.Becas.Domain.Renovacion.Datos;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Persistence.Consultas;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class SolicitudRenovacionPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public SolicitudRenovacionPersistenciaTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task PaginaSolicitudesVencidasSinOmitirLasDeLaMismaFecha()
    {
        var contexto = DatosPrueba.CrearContextoOperacion(
            new DateTimeOffset(2025, 12, 31, 12, 0, 0, TimeSpan.Zero));
        var fechas = new[] { new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2) };
        var becas = fechas.Select(_ => DatosPrueba.CrearBecaActiva(
            DatosPrueba.CrearDatosBeca(), contexto)).ToArray();
        var solicitudes = becas.Select((beca, indice) =>
            SolicitudRenovacion.Solicitar(new DatosSolicitudRenovacion(
                beca.Id, beca.PeriodoVigenteActualId, Guid.CreateVersion7(),
                fechas[indice], true, true, 1500m), contexto)).ToArray();
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.AddRange(becas);
            escritura.SolicitudesRenovacion.AddRange(solicitudes);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _fixture.CrearContexto();
        var repositorio = new SolicitudRenovacionRepository(lectura);
        var primera = await repositorio.ObtenerEnCursoVencidasAsync(
            new DateOnly(2026, 1, 3), 2);
        var segunda = await repositorio.ObtenerEnCursoVencidasAsync(
            new DateOnly(2026, 1, 3), 2,
            primera[^1].FechaLimite, primera[^1].Id);

        Assert.Equal(2, primera.Count);
        Assert.Single(segunda);
        Assert.Equal(solicitudes.Select(valor => valor.Id).Order(),
            primera.Concat(segunda).Select(valor => valor.Id).Order());
    }

    [Fact]
    public async Task ReconstruyeSolicitudConReservaDecisionYReferenciaJob()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var periodoSolicitadoId = Guid.CreateVersion7();
        var contexto = DatosPrueba.CrearContextoOperacion();
        var solicitud = SolicitudRenovacion.Solicitar(new DatosSolicitudRenovacion(
            beca.Id, beca.PeriodoVigenteActualId, periodoSolicitadoId,
            new DateOnly(2026, 3, 15), true, true, 1500m), contexto);
        solicitud.RegistrarReferenciaJobVencimiento("job-renovacion-1", contexto);
        var reservaId = Guid.CreateVersion7();
        solicitud.ConfirmarReservaPresupuestaria(reservaId, 1500m, contexto);
        solicitud.Aprobar("Cumple los criterios de renovación", contexto);

        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            escritura.SolicitudesRenovacion.Add(solicitud);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _fixture.CrearContexto();
        var repositorio = new SolicitudRenovacionRepository(lectura);
        var reconstruida = await repositorio.ObtenerPorIdAsync(solicitud.Id);
        Assert.NotNull(reconstruida);
        Assert.Equal(EstadoRenovacion.Aprobada, reconstruida.Estado);
        Assert.Equal(beca.Id, reconstruida.BecaId);
        Assert.Equal(beca.PeriodoVigenteActualId, reconstruida.PeriodoOrigenId);
        Assert.Equal(periodoSolicitadoId, reconstruida.PeriodoSolicitadoId);
        Assert.Equal(reservaId, reconstruida.ReservaPresupuestoId);
        Assert.Equal(1500m, reconstruida.MontoReservadoRenovacion);
        Assert.Equal("job-renovacion-1", reconstruida.ReferenciaJobVencimientoId);
        Assert.Equal("Cumple los criterios de renovación", reconstruida.MotivoDecision);
        Assert.Equal(solicitud.Version, reconstruida.Version);
        Assert.True(await repositorio.ExistePorBecaYPeriodoAsync(
            beca.Id, periodoSolicitadoId));
        Assert.Equal(beca.CadenaBecaId,
            (await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id)).CadenaBecaId);
    }

    [Fact]
    public async Task ImpideDuplicarSolicitudDeBecaYPeriodo()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var periodoSolicitadoId = Guid.CreateVersion7();
        SolicitudRenovacion NuevaSolicitud()
            => SolicitudRenovacion.Solicitar(new DatosSolicitudRenovacion(
                beca.Id, beca.PeriodoVigenteActualId, periodoSolicitadoId,
                new DateOnly(2026, 3, 15), true, true, 1500m),
                DatosPrueba.CrearContextoOperacion());

        await using (var primera = _fixture.CrearContexto())
        {
            primera.Becas.Add(beca);
            primera.SolicitudesRenovacion.Add(NuevaSolicitud());
            await primera.SaveChangesAsync();
        }

        await using var repeticion = _fixture.CrearContexto();
        repeticion.SolicitudesRenovacion.Add(NuevaSolicitud());
        var error = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repeticion.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation,
            Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Fact]
    public async Task ReconstruyeDosBecasDeLaMismaCadenaConSnapshotsDistintos()
    {
        var anterior = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using (var inicio = _fixture.CrearContexto())
        {
            inicio.Becas.Add(anterior);
            await inicio.SaveChangesAsync();
        }

        var periodoNuevoId = Guid.CreateVersion7();
        var costoNuevoId = Guid.CreateVersion7();
        var contexto = DatosPrueba.CrearContextoOperacion();
        var condicionesNuevas = new CondicionesPermanencia(15m, 4, 1, 40,
            ConsecuenciaPermanencia.LlamadoAtencion);
        Guid sucesoraId;
        await using (var escritura = _fixture.CrearContexto())
        {
            var beca = await new BecaRepository(escritura).ObtenerPorIdAsync(anterior.Id);
            Assert.NotNull(beca);
            beca.IniciarRenovacion(contexto);
            var solicitud = SolicitudRenovacion.Solicitar(new DatosSolicitudRenovacion(
                beca.Id, beca.PeriodoVigenteActualId, periodoNuevoId,
                new DateOnly(2026, 3, 15), true, true, 1600m), contexto);
            var reservaId = Guid.CreateVersion7();
            solicitud.ConfirmarReservaPresupuestaria(reservaId, 1600m, contexto);
            var sucesora = beca.CrearSucesoraPorRenovacion(new DatosBecaSucesora(
                periodoNuevoId, costoNuevoId, 1600m, reservaId,
                new PoliticaCobertura(TipoCobertura.Parcial, 60m, 0m, "USD"),
                condicionesNuevas, false, false,
                $"BECA-{Guid.NewGuid():N}"[..20], null), contexto);
            sucesoraId = sucesora.Id;
            var decision = DatosPrueba.CrearContextoOperacion();
            solicitud.Aprobar("Rendimiento y reserva confirmados", decision);
            beca.Renovar(new Ascenso.Becas.Domain.Otorgamiento.ValueObjects.ContextoOperacion(
                decision.UsuarioAutorId, decision.CorrelacionId, decision.Fecha,
                "Renovación aprobada por coordinación."));
            escritura.SolicitudesRenovacion.Add(solicitud);
            escritura.Becas.Add(sucesora);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _fixture.CrearContexto();
        var cadena = await lectura.Becas.AsNoTracking()
            .Where(valor => valor.CadenaBecaId == anterior.CadenaBecaId)
            .OrderBy(valor => valor.PeriodosCursados)
            .ToListAsync();
        Assert.Equal(2, cadena.Count);
        Assert.Equal(EstadoBeca.Renovada, cadena[0].Estado);
        Assert.Equal(EstadoBeca.Activa, cadena[1].Estado);
        Assert.Equal(sucesoraId, cadena[1].Id);
        Assert.Equal(cadena[0].Id, cadena[1].BecaAnteriorId);
        Assert.Equal(cadena[0].PeriodosCursados + 1, cadena[1].PeriodosCursados);
        Assert.Equal(costoNuevoId, cadena[1].CostoMatriculaReferenciaSnapshotId);
        Assert.Equal(60m, cadena[1].CoberturaAplicada.PorcentajeCobertura);
        Assert.Equal(15m,
            cadena[1].CondicionesPermanenciaSnapshot?.PromedioMinimoRenovacion);
        Assert.NotEqual(cadena[0].CondicionesPermanenciaSnapshot,
            cadena[1].CondicionesPermanenciaSnapshot);
        Assert.Null(cadena[1].Oferta);
        Assert.Equal(EstadoRenovacion.Aprobada,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.BecaId == anterior.Id)).Estado);
    }

    [Fact]
    public async Task ConsultaRenovacionesPaginadasSoloDeLaBecaSolicitada()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var otra = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var contexto = DatosPrueba.CrearContextoOperacion();
        var solicitudes = Enumerable.Range(0, 3).Select(_ =>
            SolicitudRenovacion.Solicitar(new DatosSolicitudRenovacion(
                beca.Id, beca.PeriodoVigenteActualId, Guid.CreateVersion7(),
                new DateOnly(2026, 3, 15), true, true, 1500m), contexto)).ToArray();
        var ajena = SolicitudRenovacion.Solicitar(new DatosSolicitudRenovacion(
            otra.Id, otra.PeriodoVigenteActualId, Guid.CreateVersion7(),
            new DateOnly(2026, 3, 15), true, true, 1500m), contexto);
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.AddRange(beca, otra);
            escritura.SolicitudesRenovacion.AddRange([.. solicitudes, ajena]);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _fixture.CrearContexto();
        var consultas = new ConsultasRenovacion(lectura);
        var primera = await consultas.ListarPorBecaAsync(beca.Id, 1, 2);
        var segunda = await consultas.ListarPorBecaAsync(beca.Id, 2, 2);

        Assert.Equal(3, primera.Total);
        Assert.Equal(2, primera.Elementos.Count);
        Assert.Single(segunda.Elementos);
        Assert.All(primera.Elementos.Concat(segunda.Elementos), valor =>
            Assert.Equal(beca.Id, valor.BecaId));
        Assert.Equal(3, primera.Elementos.Concat(segunda.Elementos)
            .Select(valor => valor.Id).Distinct().Count());
        Assert.Null(await consultas.ObtenerAsync(Guid.CreateVersion7()));
        Assert.Equal(ajena.Id, (await consultas.ObtenerAsync(ajena.Id))?.Id);
    }
}
