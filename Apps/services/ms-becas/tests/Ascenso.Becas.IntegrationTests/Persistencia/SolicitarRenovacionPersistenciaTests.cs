using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Renovacion.Comandos.EvaluarRenovacion;
using Ascenso.Becas.Application.Renovacion.Comandos.SolicitarRenovacion;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Application.Renovacion.Servicios;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Renovacion.Eventos;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class SolicitarRenovacionPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public SolicitarRenovacionPersistenciaTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task SolicitaYRechazaSinReservaConBecaConsistente()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var reporte = CrearReporteAprobado(beca);
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            escritura.ReportesAcademicos.Add(reporte);
            await escritura.SaveChangesAsync();
        }

        await using var contexto = _fixture.CrearContexto();
        var becas = new BecaRepository(contexto);
        var solicitudes = new SolicitudRenovacionRepository(contexto);
        var reportes = new ReporteAcademicoRepository(contexto);
        var unidad = new UnidadDeTrabajo(contexto);
        var periodoSolicitadoId = Guid.CreateVersion7();
        var ventanas = Substitute.For<IVentanaRenovacionPrograma>();
        ventanas.ObtenerAsync(beca.ProgramaBecaId, periodoSolicitadoId,
            Arg.Any<CancellationToken>())
            .Returns(new VentanaRenovacionPrograma(
                new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 15), true));
        var presupuestos = Substitute.For<IPresupuestoRenovacionPrograma>();
        presupuestos.TieneDisponibilidadAsync(beca.ProgramaBecaId,
            periodoSolicitadoId, 1500m, Arg.Any<CancellationToken>())
            .Returns(true);
        var contextoActual = Substitute.For<IContextoOperacionActual>();
        contextoActual.UsuarioId.Returns(Guid.CreateVersion7());
        contextoActual.CorrelationId.Returns(Guid.CreateVersion7().ToString());
        var reloj = Substitute.For<Ascenso.Becas.Domain.Ports.IClock>();
        reloj.UtcNow.Returns(DatosPrueba.FechaBase);
        var elegibilidad = new EvaluadorElegibilidadRenovacion(
            becas, reportes, ventanas, presupuestos, reloj);
        var outbox = Substitute.For<Ascenso.Becas.Application.Otorgamiento.Puertos.IOutboxBeca>();
        var solicitar = new SolicitarRenovacionHandler(elegibilidad,
            becas, solicitudes, unidad, contextoActual, reloj, outbox);

        var resultado = await solicitar.HandleAsync(new SolicitarRenovacionCommand(
            beca.Id, periodoSolicitadoId, 1500m));
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso, resultado.Estado);
        await outbox.Received(1).EncolarAsync(
            Arg.Is<RenovacionBecaSolicitadaEvento>(evento =>
                evento.SolicitudRenovacionId == resultado.Id),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
        Assert.True((await contexto.Becas.SingleAsync(valor => valor.Id == beca.Id))
            .TieneRenovacionEnCurso);
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            solicitar.HandleAsync(new SolicitarRenovacionCommand(
                beca.Id, periodoSolicitadoId, 1500m)));

        var evaluar = new EvaluarRenovacionHandler(
            solicitudes, becas, unidad, contextoActual, reloj);
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            evaluar.HandleAsync(new EvaluarRenovacionCommand(resultado.Id,
                true, "Aprobación pendiente de reserva y sucesora")));
        var rechazada = await evaluar.HandleAsync(new EvaluarRenovacionCommand(
            resultado.Id, false, "Incumplimiento documentado por coordinación"));
        Assert.Equal(EstadoRenovacion.Rechazada, rechazada.Estado);

        await using var lectura = _fixture.CrearContexto();
        var becaPersistida = await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id);
        var solicitudPersistida = await lectura.SolicitudesRenovacion
            .SingleAsync(valor => valor.Id == resultado.Id);
        Assert.Equal(EstadoBeca.Activa, becaPersistida.Estado);
        Assert.False(becaPersistida.TieneRenovacionEnCurso);
        Assert.Equal(EstadoRenovacion.Rechazada, solicitudPersistida.Estado);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));

        var periodoFueraDeVentanaId = Guid.CreateVersion7();
        ventanas.ObtenerAsync(beca.ProgramaBecaId, periodoFueraDeVentanaId,
            Arg.Any<CancellationToken>())
            .Returns(new VentanaRenovacionPrograma(
                new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 15), true));
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            solicitar.HandleAsync(new SolicitarRenovacionCommand(
                beca.Id, periodoFueraDeVentanaId, 1500m)));
        Assert.False(await lectura.SolicitudesRenovacion.AnyAsync(valor =>
            valor.BecaId == beca.Id
            && valor.PeriodoSolicitadoId == periodoFueraDeVentanaId));
    }

    private static ReporteAcademico CrearReporteAprobado(Beca beca)
    {
        var contexto = DatosPrueba.CrearContextoOperacion();
        var reporte = ReporteAcademico.Registrar(new DatosReporteAcademico(
            beca.Id, beca.PeriodoVigenteActualId, 17m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1), 40,
            "gs://becas/servicio/constancia.pdf", new string('b', 64)), contexto);
        reporte.IniciarRevision(contexto);
        reporte.Evaluar(beca.Oferta!.CondicionesSnapshot, contexto);
        reporte.DecidirConsecuencia(true, "Cumple las condiciones",
            "Solicitar renovación del periodo siguiente", contexto);
        reporte.ResolverRevision(true, contexto);
        return reporte;
    }
}
