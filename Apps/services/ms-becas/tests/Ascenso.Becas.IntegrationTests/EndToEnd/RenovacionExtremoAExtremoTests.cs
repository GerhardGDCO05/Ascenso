using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Eventos;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.EndToEnd;

[Collection(ColeccionExtremoAExtremo.Nombre)]
public sealed class RenovacionExtremoAExtremoTests
{
    private readonly ExtremoAExtremoFixture _fixture;

    public RenovacionExtremoAExtremoTests(ExtremoAExtremoFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task SolicitudYRechazoConservanBecaSinSucesora()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca,
            periodoNuevo, new DateTimeOffset(2026, 3, 1, 12, 0, 0,
                TimeSpan.Zero));
        using var becario = CrearCliente(fabrica, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId);
        var solicitudId = await SolicitarAsync(becario, beca.Id, periodoNuevo);

        using var coordinador = CrearCliente(fabrica, "COORDINADOR_PROGRAMA",
            programaId: beca.ProgramaBecaId, con2Fa: true);
        var rechazo = await coordinador.PostAsJsonAsync(
            $"/api/v1/renovaciones/{solicitudId}/evaluacion",
            new { aprobar = false, motivoDecision = "Rendimiento insuficiente comprobado" });

        Assert.Equal(HttpStatusCode.OK, rechazo.StatusCode);
        await using var lectura = _fixture.CrearContexto();
        var actual = await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id);
        Assert.Equal(EstadoBeca.Activa, actual.Estado);
        Assert.False(actual.TieneRenovacionEnCurso);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
        Assert.Equal(EstadoRenovacion.Rechazada,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.Id == solicitudId)).Estado);
        Assert.Contains(baseFabrica.Outbox.Eventos.OfType<
            RenovacionBecaSolicitadaEvento>(), valor =>
            valor.SolicitudRenovacionId == solicitudId);
    }

    [Fact]
    public async Task AprobacionSinReservaRealNoCreaSucesoraNiAlteraMontos()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca,
            periodoNuevo, new DateTimeOffset(2026, 3, 1, 12, 0, 0,
                TimeSpan.Zero));
        using var becario = CrearCliente(fabrica, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId);
        var solicitudId = await SolicitarAsync(becario, beca.Id, periodoNuevo);

        using var coordinador = CrearCliente(fabrica, "COORDINADOR_PROGRAMA",
            programaId: beca.ProgramaBecaId, con2Fa: true);
        var aprobacion = await coordinador.PostAsJsonAsync(
            $"/api/v1/renovaciones/{solicitudId}/evaluacion",
            new { aprobar = true, motivoDecision = "Cumple condiciones académicas" });

        Assert.Equal(HttpStatusCode.Conflict, aprobacion.StatusCode);
        await using var lectura = _fixture.CrearContexto();
        var actual = await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id);
        Assert.Equal(EstadoBeca.Activa, actual.Estado);
        Assert.Equal(1500m, actual.MontoComprometidoPeriodoActual);
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.Id == solicitudId)).Estado);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
    }

    [Fact]
    public async Task AprobacionConPuertosConfirmadosCreaDosPeriodosYCalendario()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        var costoNuevo = Guid.CreateVersion7();
        var reservaId = Guid.CreateVersion7();
        var programas = Substitute.For<IProgramasClient>();
        programas.ReservarAsync(beca.ProgramaBecaId,
                Arg.Is<SolicitudReservaPresupuesto>(valor =>
                    valor.ReferenciaTipo == "RENOVACION"
                    && valor.PeriodoAcademicoId == periodoNuevo
                    && valor.Monto == 1600m && valor.Cupos == 1),
                Arg.Any<CancellationToken>())
            .Returns(new ResultadoReservaPresupuesto(reservaId,
                "PENDIENTE", 1600m, 1,
                new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero)));
        var plan = new PlanNuevoPeriodo(beca.ProgramaBecaId, periodoNuevo,
            costoNuevo, 2666.67m,
            new PoliticaCobertura(TipoCobertura.Parcial, 60m, 0m,
                "USD"), new CondicionesPermanencia(15m, 4, 1, 40,
                ConsecuenciaPermanencia.LlamadoAtencion), false, false, null,
            FrecuenciaDesembolso.Semestral, DestinatarioPago.Becario,
            new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 31),
            [new CuotaProgramada(1, new DateOnly(2026, 4, 15), 1600m)]);
        var planes = Substitute.For<IPlanNuevoPeriodoPrograma>();
        planes.ObtenerAsync(beca.ProgramaBecaId, periodoNuevo,
                Arg.Any<CancellationToken>()).Returns(plan);
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero),
            planes: planes, programas: programas);
        using var becario = CrearCliente(fabrica, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId);
        var solicitudId = await SolicitarAsync(becario, beca.Id, periodoNuevo);
        using var coordinador = CrearCliente(fabrica, "COORDINADOR_PROGRAMA",
            programaId: beca.ProgramaBecaId, con2Fa: true);
        var respuesta = await coordinador.PostAsJsonAsync(
            $"/api/v1/renovaciones/{solicitudId}/evaluacion",
            new { aprobar = true, motivoDecision = "Cumple condiciones del periodo" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var lectura = _fixture.CrearContexto();
        var anterior = await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id);
        var sucesora = await lectura.Becas.SingleAsync(valor =>
            valor.BecaAnteriorId == beca.Id);
        Assert.Equal(EstadoBeca.Renovada, anterior.Estado);
        Assert.Equal(EstadoBeca.Activa, sucesora.Estado);
        Assert.Equal(anterior.CadenaBecaId, sucesora.CadenaBecaId);
        Assert.Equal(periodoNuevo, sucesora.PeriodoVigenteActualId);
        Assert.Equal(anterior.PeriodosCursados + 1, sucesora.PeriodosCursados);
        Assert.Equal(1600m, sucesora.MontoComprometidoPeriodoActual);
        Assert.Equal(costoNuevo, sucesora.CostoMatriculaReferenciaSnapshotId);
        Assert.Equal(1500m, anterior.MontoComprometidoPeriodoActual);
        var solicitud = await lectura.SolicitudesRenovacion.SingleAsync(valor =>
            valor.Id == solicitudId);
        Assert.Equal(EstadoRenovacion.Aprobada, solicitud.Estado);
        Assert.Equal(1600m, solicitud.MontoSolicitado);
        Assert.Equal(reservaId, solicitud.ReservaPresupuestoId);
        var calendario = await lectura.CalendariosDesembolso.SingleAsync(valor =>
            valor.BecaId == sucesora.Id);
        Assert.Equal(1600m, calendario.MontoMaximoAprobadoPeriodo);
        Assert.Contains(baseFabrica.Outbox.Eventos.OfType<RenovacionBecaAprobadaEvento>(),
            valor => valor.SolicitudRenovacionId == solicitudId
                && valor.BecaSucesoraId == sucesora.Id);
        Assert.Contains(baseFabrica.Outbox.Eventos.OfType<BecaActivadaNuevoPeriodoEvento>(),
            valor => valor.BecaId == sucesora.Id);
        await programas.Received(1).ConfirmarReservaAsync(beca.ProgramaBecaId,
            reservaId, solicitudId, Arg.Any<CancellationToken>());
        await programas.Received(1).ActivarReservaAsync(beca.ProgramaBecaId,
            reservaId, solicitudId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservaInvalidaNoApruebaNiCreaSucesora()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        var planes = Substitute.For<IPlanNuevoPeriodoPrograma>();
        planes.ObtenerAsync(beca.ProgramaBecaId, periodoNuevo,
                Arg.Any<CancellationToken>())
            .Returns(new PlanNuevoPeriodo(beca.ProgramaBecaId, periodoNuevo,
                Guid.CreateVersion7(), 2000m,
                new PoliticaCobertura(TipoCobertura.MontoFijo, 0m, 1600m, "USD"),
                beca.Oferta!.CondicionesSnapshot, false, false, null,
                FrecuenciaDesembolso.Semestral, DestinatarioPago.Becario,
                new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 31),
                [new CuotaProgramada(1, new DateOnly(2026, 4, 15), 1600m)]));
        var programas = Substitute.For<IProgramasClient>();
        programas.ReservarAsync(beca.ProgramaBecaId,
                Arg.Any<SolicitudReservaPresupuesto>(), Arg.Any<CancellationToken>())
            .Returns(new ResultadoReservaPresupuesto(Guid.Empty,
                "PENDIENTE", 1600m, 1, DateTimeOffset.UtcNow));
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero),
            planes: planes, programas: programas);
        using var becario = CrearCliente(fabrica, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId);
        var solicitudId = await SolicitarAsync(becario, beca.Id, periodoNuevo);
        using var coordinador = CrearCliente(fabrica, "COORDINADOR_PROGRAMA",
            programaId: beca.ProgramaBecaId, con2Fa: true);

        var respuesta = await coordinador.PostAsJsonAsync(
            $"/api/v1/renovaciones/{solicitudId}/evaluacion",
            new { aprobar = true, motivoDecision = "Cumple condiciones" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoBeca.Activa,
            (await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id)).Estado);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.Id == solicitudId)).Estado);
    }

    [Fact]
    public async Task VencimientoSinSolicitudConcluyeSinCrearRenovacion()
    {
        var beca = await SembrarBecaAsync(conReporte: false);
        var periodoNuevo = Guid.CreateVersion7();
        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        cupos.LiberarPorConclusionAsync(beca.ProgramaBecaId, beca.Id,
            beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca,
            periodoNuevo, new DateTimeOffset(2026, 3, 16, 12, 0, 0,
                TimeSpan.Zero), cupos);
        using var cliente = fabrica.CreateClient();
        using var alcance = fabrica.Services.CreateScope();
        var job = alcance.ServiceProvider.GetRequiredService<ConcluirBecaJob>();

        await job.Ejecutar(beca.Id, periodoNuevo);
        await job.Ejecutar(beca.Id, periodoNuevo);

        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoBeca.Concluida,
            (await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id)).Estado);
        Assert.False(await lectura.SolicitudesRenovacion.AnyAsync(valor =>
            valor.BecaId == beca.Id));
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
        await cupos.Received(1).LiberarPorConclusionAsync(
            beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReconciliacionDeSolicitudVencidaConcluyeUnaSolaVez()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        using (var origen = new FabricaApiExtremoAExtremo(_fixture))
        using (var inicial = CrearFabrica(origen, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero)))
        using (var becario = CrearCliente(inicial, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId))
        {
            await SolicitarAsync(becario, beca.Id, periodoNuevo);
        }

        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        cupos.LiberarPorConclusionAsync(beca.ProgramaBecaId, beca.Id,
                beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero), cupos);
        using var cliente = fabrica.CreateClient();
        using var alcance = fabrica.Services.CreateScope();
        var job = alcance.ServiceProvider.GetRequiredService<ReconciliarRenovacionesJob>();
        var ventana = await alcance.ServiceProvider
            .GetRequiredService<IVentanaRenovacionPrograma>()
            .ObtenerAsync(beca.ProgramaBecaId, periodoNuevo);
        Assert.NotNull(ventana);

        await job.Ejecutar();
        await job.Ejecutar();

        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoBeca.Concluida,
            (await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id)).Estado);
        Assert.Equal(EstadoRenovacion.ExpiradaConcluida,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.BecaId == beca.Id)).Estado);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
        await cupos.Received(1).LiberarPorConclusionAsync(beca.ProgramaBecaId,
            beca.Id, beca.PeriodoVigenteActualId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReconciliacionRespetaRenovacionTardiaExcepcional()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        using (var origen = new FabricaApiExtremoAExtremo(_fixture))
        using (var inicial = CrearFabrica(origen, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero)))
        using (var becario = CrearCliente(inicial, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId))
        {
            await SolicitarAsync(becario, beca.Id, periodoNuevo);
        }

        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero),
            cupos, permiteRenovacionTardia: true);
        using var cliente = fabrica.CreateClient();
        using var alcance = fabrica.Services.CreateScope();

        await alcance.ServiceProvider.GetRequiredService<ReconciliarRenovacionesJob>()
            .Ejecutar();

        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoBeca.Activa,
            (await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id)).Estado);
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.BecaId == beca.Id)).Estado);
        await cupos.DidNotReceive().LiberarPorConclusionAsync(
            beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReconciliacionConVentanaInactivaConservaSolicitudYBeca()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        using (var origen = new FabricaApiExtremoAExtremo(_fixture))
        using (var inicial = CrearFabrica(origen, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero)))
        using (var becario = CrearCliente(inicial, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId))
        {
            await SolicitarAsync(becario, beca.Id, periodoNuevo);
        }

        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero),
            cupos, ventanaActiva: false);
        using var cliente = fabrica.CreateClient();
        using var alcance = fabrica.Services.CreateScope();

        await alcance.ServiceProvider.GetRequiredService<ReconciliarRenovacionesJob>()
            .Ejecutar();

        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoBeca.Activa,
            (await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id)).Estado);
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.BecaId == beca.Id)).Estado);
        await cupos.DidNotReceive().LiberarPorConclusionAsync(
            beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AprobacionRevalidaFinOficialModificadoAntesDeReservar()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        Guid solicitudId;
        using (var origen = new FabricaApiExtremoAExtremo(_fixture))
        using (var inicial = CrearFabrica(origen, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero)))
        using (var becario = CrearCliente(inicial, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId))
        {
            solicitudId = await SolicitarAsync(becario, beca.Id, periodoNuevo);
        }

        var programas = Substitute.For<IProgramasClient>();
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca, periodoNuevo,
            new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero),
            programas: programas, finVentana: new DateOnly(2026, 3, 5));
        using var coordinador = CrearCliente(fabrica, "COORDINADOR_PROGRAMA",
            programaId: beca.ProgramaBecaId, con2Fa: true);

        var respuesta = await coordinador.PostAsJsonAsync(
            $"/api/v1/renovaciones/{solicitudId}/evaluacion",
            new { aprobar = true, motivoDecision = "Cumple condiciones" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        await programas.DidNotReceiveWithAnyArgs().ReservarAsync(default,
            default!, default);
        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.Id == solicitudId)).Estado);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
    }

    private async Task<Beca> SembrarBecaAsync(bool conReporte)
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using var contexto = _fixture.CrearContexto();
        contexto.Becas.Add(beca);
        if (conReporte)
            contexto.ReportesAcademicos.Add(CrearReporteAprobado(beca));
        await contexto.SaveChangesAsync();
        return beca;
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

    private static WebApplicationFactory<Program> CrearFabrica(
        FabricaApiExtremoAExtremo fabrica, Beca beca, Guid periodoNuevo,
        DateTimeOffset fecha, ILiberacionCupoPrograma? cupos = null,
        IPlanNuevoPeriodoPrograma? planes = null,
        IProgramasClient? programas = null,
        bool permiteRenovacionTardia = false,
        bool ventanaActiva = true, DateOnly? finVentana = null)
        => fabrica.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
        {
            var reloj = Substitute.For<IClock>();
            reloj.UtcNow.Returns(fecha);
            var ventanas = Substitute.For<IVentanaRenovacionPrograma>();
            ventanas.ObtenerAsync(beca.ProgramaBecaId, periodoNuevo,
                Arg.Any<CancellationToken>())
                .Returns(new VentanaRenovacionPrograma(
                    new DateOnly(2026, 2, 28),
                    finVentana ?? new DateOnly(2026, 3, 15),
                    ventanaActiva, permiteRenovacionTardia));
            var presupuestos = Substitute.For<IPresupuestoRenovacionPrograma>();
            presupuestos.TieneDisponibilidadAsync(beca.ProgramaBecaId,
                periodoNuevo, 1600m, Arg.Any<CancellationToken>())
                .Returns(true);
            var periodos = Substitute.For<IPeriodosAcademicosClient>();
            periodos.ObtenerActualAsync(Arg.Any<CancellationToken>())
                .Returns(new ResultadoPeriodoAcademico(
                    beca.PeriodoVigenteActualId, "2026-1", "Periodo vigente", 1,
                    new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31),
                    "ACTIVO", true));
            servicios.RemoveAll<IClock>();
            servicios.AddSingleton(reloj);
            servicios.RemoveAll<IVentanaRenovacionPrograma>();
            servicios.AddSingleton(ventanas);
            servicios.RemoveAll<IPresupuestoRenovacionPrograma>();
            servicios.AddSingleton(presupuestos);
            servicios.RemoveAll<IPeriodosAcademicosClient>();
            servicios.AddSingleton(periodos);
            servicios.RemoveAll<ILiberacionCupoPrograma>();
            servicios.AddSingleton(cupos ?? Substitute.For<ILiberacionCupoPrograma>());
            if (planes is not null)
            {
                servicios.RemoveAll<IPlanNuevoPeriodoPrograma>();
                servicios.AddSingleton(planes);
            }
            if (programas is not null)
            {
                servicios.RemoveAll<IProgramasClient>();
                servicios.AddSingleton(programas);
            }
        }));

    private static HttpClient CrearCliente(WebApplicationFactory<Program> fabrica,
        string rol, Guid? postulanteId = null, Guid? programaId = null,
        bool con2Fa = false)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Test-Roles", rol);
        if (postulanteId.HasValue)
            cliente.DefaultRequestHeaders.Add("X-Test-Postulante",
                postulanteId.Value.ToString());
        if (programaId.HasValue)
            cliente.DefaultRequestHeaders.Add("X-Test-Programas",
                programaId.Value.ToString());
        if (con2Fa)
            cliente.DefaultRequestHeaders.Add("X-Test-Acr", "urn:ascenso:loa:2fa");
        cliente.DefaultRequestHeaders.Add("Idempotency-Key",
            Guid.CreateVersion7().ToString());
        return cliente;
    }

    private static async Task<Guid> SolicitarAsync(HttpClient cliente,
        Guid becaId, Guid periodoNuevo)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/renovaciones",
            new { periodoSolicitadoId = periodoNuevo, montoSolicitado = 1600m });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var contenido = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        return contenido.RootElement.GetProperty("id").GetGuid();
    }
}
