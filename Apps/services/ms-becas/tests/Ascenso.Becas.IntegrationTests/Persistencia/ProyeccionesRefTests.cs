using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Persistence.Proyecciones;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ProyeccionesRefTests
{
    private readonly PostgreSqlFixture _fixture;

    public ProyeccionesRefTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_AplicarUpsertIdempotenteEnPrograma_Cuando_SeRepiteElEvento()
    {
        await using var contexto = _fixture.CrearContexto();
        var actualizador = new ActualizadorProyeccionesRef(contexto);
        var programa = CrearPrograma("Programa original", _fechaBase);

        Assert.Equal(1, await actualizador.ActualizarProgramaAsync(programa));

        programa.Nombre = "No debe aplicarse";
        Assert.Equal(0, await actualizador.ActualizarProgramaAsync(programa));

        var eventoAnterior = CrearPrograma(
            "Evento anterior",
            _fechaBase.AddMinutes(-1),
            programa.ProgramaBecaId);
        Assert.Equal(0, await actualizador.ActualizarProgramaAsync(eventoAnterior));

        var eventoNuevo = CrearPrograma(
            "Programa actualizado",
            _fechaBase.AddMinutes(1),
            programa.ProgramaBecaId);
        Assert.Equal(1, await actualizador.ActualizarProgramaAsync(eventoNuevo));

        var guardado = await contexto.ProgramasBecaRef
            .AsNoTracking()
            .SingleAsync(registro => registro.ProgramaBecaId == programa.ProgramaBecaId);

        Assert.Equal("Programa actualizado", guardado.Nombre);
        Assert.Equal(eventoNuevo.UltimoEventoId, guardado.UltimoEventoId);
        Assert.Equal(eventoNuevo.OccurredAt, guardado.OccurredAt);
        Assert.NotEqual(default, guardado.FechaUltimaActualizacion);
    }

    [Fact]
    public async Task Debe_AplicarUpsertIdempotenteEnCostoDeMatricula_Cuando_SeRepiteElEvento()
    {
        await using var contexto = _fixture.CrearContexto();
        var actualizador = new ActualizadorProyeccionesRef(contexto);
        var costo = CrearCosto(1200.50m, _fechaBase, fechaVigenciaHasta: null);

        Assert.Equal(1, await actualizador.ActualizarCostoMatriculaAsync(costo));

        costo.MontoReferenciaMatricula = 9999m;
        Assert.Equal(0, await actualizador.ActualizarCostoMatriculaAsync(costo));

        var eventoAnterior = CrearCosto(
            1000m,
            _fechaBase.AddMinutes(-1),
            new DateOnly(2026, 12, 31),
            costo.CostoMatriculaReferenciaId);
        Assert.Equal(0, await actualizador.ActualizarCostoMatriculaAsync(eventoAnterior));

        var eventoNuevo = CrearCosto(
            1500.75m,
            _fechaBase.AddMinutes(1),
            new DateOnly(2027, 6, 30),
            costo.CostoMatriculaReferenciaId);
        Assert.Equal(1, await actualizador.ActualizarCostoMatriculaAsync(eventoNuevo));

        var guardado = await contexto.CostosMatriculaRef
            .AsNoTracking()
            .SingleAsync(registro =>
                registro.CostoMatriculaReferenciaId == costo.CostoMatriculaReferenciaId);

        Assert.Equal(1500.75m, guardado.MontoReferenciaMatricula);
        Assert.Equal(new DateOnly(2027, 6, 30), guardado.FechaVigenciaHasta);
        Assert.Equal(eventoNuevo.UltimoEventoId, guardado.UltimoEventoId);
    }

    [Fact]
    public async Task Debe_AplicarUpsertIdempotenteEnConvenio_Cuando_SeRepiteElEvento()
    {
        await using var contexto = _fixture.CrearContexto();
        var actualizador = new ActualizadorProyeccionesRef(contexto);
        var convenio = CrearConvenio("VIGENTE", new DateOnly(2026, 12, 31), _fechaBase);

        Assert.Equal(1, await actualizador.ActualizarConvenioAsync(convenio));

        convenio.Estado = "NO_DEBE_APLICARSE";
        Assert.Equal(0, await actualizador.ActualizarConvenioAsync(convenio));

        var eventoAnterior = CrearConvenio(
            "VENCIDO",
            new DateOnly(2026, 1, 1),
            _fechaBase.AddMinutes(-1),
            convenio.UniversidadAliadaId);
        Assert.Equal(0, await actualizador.ActualizarConvenioAsync(eventoAnterior));

        var eventoNuevo = CrearConvenio(
            "VENCIDO",
            new DateOnly(2027, 1, 1),
            _fechaBase.AddMinutes(1),
            convenio.UniversidadAliadaId);
        Assert.Equal(1, await actualizador.ActualizarConvenioAsync(eventoNuevo));

        var guardado = await contexto.ConveniosRef
            .AsNoTracking()
            .SingleAsync(registro =>
                registro.UniversidadAliadaId == convenio.UniversidadAliadaId);

        Assert.Equal("VENCIDO", guardado.Estado);
        Assert.Equal(new DateOnly(2027, 1, 1), guardado.FechaFin);
        Assert.Equal(eventoNuevo.UltimoEventoId, guardado.UltimoEventoId);
    }

    private static readonly DateTimeOffset _fechaBase =
        new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static ProgramaBecaRef CrearPrograma(
        string nombre,
        DateTimeOffset occurredAt,
        Guid? programaBecaId = null)
        => new()
        {
            ProgramaBecaId = programaBecaId ?? Guid.CreateVersion7(),
            Codigo = $"PROG-{Guid.NewGuid():N}"[..20],
            Nombre = nombre,
            PoliticaCobertura = new PoliticaCobertura(
                TipoCobertura.Parcial,
                50m,
                0m,
                "USD"),
            CondicionesPermanencia = new CondicionesPermanencia(
                13m,
                4,
                1,
                40,
                ConsecuenciaPermanencia.LlamadoAtencion),
            PermiteAcumulacion = true,
            RequiereCompromisoFirmado = false,
            DuracionPeriodos = 2,
            Estado = "ACTIVO",
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = occurredAt,
            FechaUltimaActualizacion = occurredAt
        };

    private static CostoMatriculaRef CrearCosto(
        decimal monto,
        DateTimeOffset occurredAt,
        DateOnly? fechaVigenciaHasta,
        Guid? costoMatriculaReferenciaId = null)
        => new()
        {
            CostoMatriculaReferenciaId =
                costoMatriculaReferenciaId ?? Guid.CreateVersion7(),
            CarreraId = Guid.CreateVersion7(),
            PeriodoAcademicoId = Guid.CreateVersion7(),
            MontoReferenciaMatricula = monto,
            Moneda = "USD",
            NumeroVersion = 1,
            FechaVigenciaDesde = new DateOnly(2026, 1, 1),
            FechaVigenciaHasta = fechaVigenciaHasta,
            Estado = "VIGENTE",
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = occurredAt,
            FechaUltimaActualizacion = occurredAt
        };

    private static ConvenioRef CrearConvenio(
        string estado,
        DateOnly fechaFin,
        DateTimeOffset occurredAt,
        Guid? universidadAliadaId = null)
        => new()
        {
            UniversidadAliadaId = universidadAliadaId ?? Guid.CreateVersion7(),
            ConvenioId = Guid.CreateVersion7(),
            Estado = estado,
            FechaFin = fechaFin,
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = occurredAt,
            FechaUltimaActualizacion = occurredAt
        };
}
