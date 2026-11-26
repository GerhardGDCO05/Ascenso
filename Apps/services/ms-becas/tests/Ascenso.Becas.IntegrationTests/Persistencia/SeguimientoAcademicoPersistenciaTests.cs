using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.AplicarConsecuenciaPermanencia;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.ConfirmarEvaluacionPermanencia;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.EvaluarPermanencia;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.RegistrarReporteAcademico;
using Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerEvaluacionPermanencia;
using Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerReporteAcademico;
using Ascenso.Becas.Application.SeguimientoAcademico.Puertos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class SeguimientoAcademicoPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public SeguimientoAcademicoPersistenciaTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Theory]
    [InlineData(FuenteReporte.Becario, false)]
    [InlineData(FuenteReporte.Universidad, true)]
    public async Task RegistraDesdeAmbasFuentesYAlertaSoloSiEsTardio(
        FuenteReporte fuente, bool tardio)
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            await escritura.SaveChangesAsync();
        }

        await using var contexto = _fixture.CrearContexto();
        var calendario = Substitute.For<ICalendarioAcademicoPrograma>();
        calendario.ObtenerFechaLimiteReporteAsync(beca.ProgramaBecaId,
            beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
            .Returns(tardio ? new DateOnly(2026, 2, 28) : new DateOnly(2026, 3, 1));
        var eventos = new List<IDomainEvent>();
        var outbox = Substitute.For<IOutboxBeca>();
        outbox.EncolarAsync(Arg.Any<IDomainEvent>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        outbox.When(valor => valor.EncolarAsync(Arg.Any<IDomainEvent>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(llamada => eventos.Add(llamada.ArgAt<IDomainEvent>(0)));
        var contextoActual = Substitute.For<IContextoOperacionActual>();
        contextoActual.UsuarioId.Returns(Guid.CreateVersion7());
        contextoActual.CorrelationId.Returns(Guid.CreateVersion7().ToString());
        var reloj = Substitute.For<IClock>();
        reloj.UtcNow.Returns(DatosPrueba.FechaBase);
        var registrar = new RegistrarReporteAcademicoHandler(
            new BecaRepository(contexto), new ReporteAcademicoRepository(contexto),
            calendario, new UnidadDeTrabajo(contexto), outbox, contextoActual, reloj);

        var resultado = await registrar.HandleAsync(CrearComandoRegistro(beca.Id,
            beca.PeriodoVigenteActualId, fuente));

        Assert.Equal(tardio, resultado.EntregaTardia);
        Assert.Single(eventos.OfType<ReporteAcademicoEntregadoEvento>());
        Assert.Equal(tardio ? 1 : 0,
            eventos.OfType<AlertaReporteAcademicoTardioEvento>().Count());
        Assert.True(await contexto.ReportesAcademicos.AnyAsync(
            reporte => reporte.Id == resultado.Id));
    }

    [Fact]
    public async Task RechazaRegistroSinFechaLimiteOficial()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            await escritura.SaveChangesAsync();
        }

        await using var contexto = _fixture.CrearContexto();
        var calendario = Substitute.For<ICalendarioAcademicoPrograma>();
        calendario.ObtenerFechaLimiteReporteAsync(Arg.Any<Guid>(),
            Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DateOnly?)null);
        var registrar = new RegistrarReporteAcademicoHandler(
            new BecaRepository(contexto), new ReporteAcademicoRepository(contexto),
            calendario, new UnidadDeTrabajo(contexto), Substitute.For<IOutboxBeca>(),
            Substitute.For<IContextoOperacionActual>(), Substitute.For<IClock>());

        var error = await Assert.ThrowsAsync<ConfiguracionProgramaNoDisponibleException>(
            () => registrar.HandleAsync(CrearComandoRegistro(beca.Id,
                beca.PeriodoVigenteActualId, FuenteReporte.Becario)));
        Assert.Equal(422, error.EstadoHttp);
        Assert.False(await contexto.ReportesAcademicos.AnyAsync(
            reporte => reporte.BecaId == beca.Id));
    }

    [Fact]
    public async Task RechazaEvaluacionSinSnapshotDeCondiciones()
    {
        var beca = Beca.CrearOtorgada(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var reporte = ReporteAcademico.Registrar(new DatosReporteAcademico(
            beca.Id, beca.PeriodoVigenteActualId, 17m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1)),
            DatosPrueba.CrearContextoOperacion());
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            escritura.ReportesAcademicos.Add(reporte);
            await escritura.SaveChangesAsync();
        }

        await using var contexto = _fixture.CrearContexto();
        var evaluar = new EvaluarPermanenciaHandler(
            new ReporteAcademicoRepository(contexto), new BecaRepository(contexto),
            new UnidadDeTrabajo(contexto), Substitute.For<IContextoOperacionActual>(),
            Substitute.For<IClock>(), Substitute.For<IOutboxBeca>());
        var error = await Assert.ThrowsAsync<ConfiguracionProgramaNoDisponibleException>(
            () => evaluar.HandleAsync(new EvaluarPermanenciaCommand(reporte.Id)));
        Assert.Equal(422, error.EstadoHttp);
        Assert.Null((await contexto.ReportesAcademicos.AsNoTracking()
            .Include(valor => valor.Evaluacion)
            .SingleAsync(valor => valor.Id == reporte.Id)).Evaluacion);
    }

    private static RegistrarReporteAcademicoCommand CrearComandoRegistro(
        Guid becaId, Guid periodoId, FuenteReporte fuente)
        => new(becaId, periodoId, 17m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", fuente,
            Guid.CreateVersion7(), 40,
            "gs://becas/servicio/constancia.pdf", new string('b', 64));

    [Fact]
    public async Task EvaluaConfirmaYConsultaElReporteConLaFotografiaDeLaOferta()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var reporte = ReporteAcademico.Registrar(new DatosReporteAcademico(
            beca.Id, beca.PeriodoVigenteActualId, 12m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Universidad,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1), 40,
            "gs://becas/servicio/constancia.pdf", new string('b', 64)),
            DatosPrueba.CrearContextoOperacion());
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            escritura.ReportesAcademicos.Add(reporte);
            await escritura.SaveChangesAsync();
        }

        await using var contexto = _fixture.CrearContexto();
        var reportes = new ReporteAcademicoRepository(contexto);
        var usuario = Guid.CreateVersion7();
        var contextoActual = Substitute.For<IContextoOperacionActual>();
        contextoActual.UsuarioId.Returns(usuario);
        contextoActual.CorrelationId.Returns(Guid.CreateVersion7().ToString());
        var reloj = Substitute.For<IClock>();
        reloj.UtcNow.Returns(DatosPrueba.FechaBase);
        var unidad = new UnidadDeTrabajo(contexto);
        var outbox = Substitute.For<IOutboxBeca>();

        var evaluar = new EvaluarPermanenciaHandler(reportes,
            new BecaRepository(contexto), unidad, contextoActual, reloj, outbox);
        var evaluacion = await evaluar.HandleAsync(new EvaluarPermanenciaCommand(reporte.Id));
        Assert.False(evaluacion.CumpleCondiciones);
        Assert.Equal(ConsecuenciaPermanencia.LlamadoAtencion,
            evaluacion.ConsecuenciaRecomendada);
        Assert.Equal(4, evaluacion.DetalleCondiciones.Count);
        var evaluacionPersistida = await contexto.ReportesAcademicos.AsNoTracking()
            .Include(valor => valor.Evaluacion!)
            .ThenInclude(valor => valor.DetalleCondiciones)
            .SingleAsync(valor => valor.Id == reporte.Id);
        Assert.Equal(4, evaluacionPersistida.Evaluacion?.DetalleCondiciones.Count);

        var aplicar = new AplicarConsecuenciaPermanenciaHandler(
            reportes, unidad, contextoActual, reloj, outbox);
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            aplicar.HandleAsync(new AplicarConsecuenciaPermanenciaCommand(reporte.Id)));

        var confirmar = new ConfirmarEvaluacionPermanenciaHandler(
            reportes, unidad, contextoActual, reloj);
        var confirmada = await confirmar.HandleAsync(
            new ConfirmarEvaluacionPermanenciaCommand(reporte.Id, true,
                "Cumple las condiciones", "Continuar en el siguiente periodo"));
        Assert.Equal(EstadoConsecuencia.Confirmada, confirmada.EstadoConfirmacion);
        Assert.Equal(usuario, confirmada.EvaluadoPorCoordinadorId);

        var aplicada = await aplicar.HandleAsync(
            new AplicarConsecuenciaPermanenciaCommand(reporte.Id));
        Assert.Equal(ConsecuenciaPermanencia.LlamadoAtencion,
            aplicada.ConsecuenciaAplicada);
        Assert.Equal(EstadoBeca.Activa,
            (await contexto.Becas.SingleAsync(valor => valor.Id == beca.Id)).Estado);
        await outbox.Received(1).EncolarAsync(
            Arg.Is<IDomainEvent>(evento => evento is EvaluacionPermanenciaRegistradaEvento),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
        await outbox.Received(1).EncolarAsync(
            Arg.Is<IDomainEvent>(evento => evento is ConsecuenciaPermanenciaAplicadaEvento),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            aplicar.HandleAsync(new AplicarConsecuenciaPermanenciaCommand(reporte.Id)));

        var consultaReporte = await new ObtenerReporteAcademicoHandler(reportes)
            .HandleAsync(new ObtenerReporteAcademicoQuery(reporte.Id));
        var consultaEvaluacion = await new ObtenerEvaluacionPermanenciaHandler(reportes)
            .HandleAsync(new ObtenerEvaluacionPermanenciaQuery(evaluacion.Id));
        Assert.Equal(EstadoReporteAcademico.Aprobado, consultaReporte.Estado);
        Assert.Equal(FuenteReporte.Universidad, consultaReporte.FuenteEntrega);
        Assert.Equal(4, consultaEvaluacion.DetalleCondiciones.Count);
        var reportePorPeriodo = await reportes.ObtenerPorBecaYPeriodoParaConsultaAsync(
            beca.Id, beca.PeriodoVigenteActualId);
        Assert.Equal(reporte.Id, reportePorPeriodo?.Id);
    }

    [Theory]
    [InlineData(ConsecuenciaPermanencia.Suspension)]
    [InlineData(ConsecuenciaPermanencia.Revocacion)]
    public async Task ReporteTardioYEvaluacionConConsecuenciaPendienteNoCambianLaBeca(
        ConsecuenciaPermanencia consecuencia)
    {
        var datos = DatosPrueba.CrearDatosBeca();
        var operacion = DatosPrueba.CrearContextoOperacion();
        var beca = Beca.CrearOtorgada(datos, operacion);
        var oferta = beca.EmitirOferta(DatosPrueba.CrearDatosOferta(datos) with
        {
            CondicionesSnapshot = new CondicionesPermanencia(
                13m, 4, 1, 40, consecuencia)
        }, operacion);
        beca.AceptarOferta(oferta.Id, operacion);
        var historialInicial = beca.Historial.Count;
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            await escritura.SaveChangesAsync();
        }

        await using var contexto = _fixture.CrearContexto();
        var reportes = new ReporteAcademicoRepository(contexto);
        var unidad = new UnidadDeTrabajo(contexto);
        var calendario = Substitute.For<ICalendarioAcademicoPrograma>();
        calendario.ObtenerFechaLimiteReporteAsync(beca.ProgramaBecaId,
            beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
            .Returns(new DateOnly(2026, 2, 28));
        var eventos = new List<IDomainEvent>();
        var outbox = Substitute.For<IOutboxBeca>();
        outbox.EncolarAsync(Arg.Any<IDomainEvent>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        outbox.When(valor => valor.EncolarAsync(Arg.Any<IDomainEvent>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>()))
            .Do(llamada => eventos.Add(llamada.ArgAt<IDomainEvent>(0)));
        var contextoActual = Substitute.For<IContextoOperacionActual>();
        contextoActual.UsuarioId.Returns(Guid.CreateVersion7());
        contextoActual.CorrelationId.Returns(Guid.CreateVersion7().ToString());
        var reloj = Substitute.For<IClock>();
        reloj.UtcNow.Returns(DatosPrueba.FechaBase);

        var registrar = new RegistrarReporteAcademicoHandler(
            new BecaRepository(contexto), reportes, calendario, unidad,
            outbox, contextoActual, reloj);
        var comando = CrearComandoRegistro(beca.Id,
            beca.PeriodoVigenteActualId, FuenteReporte.Becario);
        var reporte = await registrar.HandleAsync(
            comando with { PromedioPeriodo = 12m });
        Assert.True(reporte.EntregaTardia);

        var evaluar = new EvaluarPermanenciaHandler(reportes,
            new BecaRepository(contexto), unidad, contextoActual, reloj, outbox);
        var evaluacion = await evaluar.HandleAsync(
            new EvaluarPermanenciaCommand(reporte.Id));
        Assert.Equal(consecuencia, evaluacion.ConsecuenciaRecomendada);

        var confirmar = new ConfirmarEvaluacionPermanenciaHandler(
            reportes, unidad, contextoActual, reloj);
        await confirmar.HandleAsync(new ConfirmarEvaluacionPermanenciaCommand(
            reporte.Id, true, "Promedio menor al mínimo",
            "Presentar plan de recuperación académica"));

        var aplicar = new AplicarConsecuenciaPermanenciaHandler(
            reportes, unidad, contextoActual, reloj, outbox);
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            aplicar.HandleAsync(new AplicarConsecuenciaPermanenciaCommand(reporte.Id)));

        Assert.Single(eventos.OfType<ReporteAcademicoEntregadoEvento>());
        Assert.Single(eventos.OfType<AlertaReporteAcademicoTardioEvento>());
        Assert.Single(eventos.OfType<EvaluacionPermanenciaRegistradaEvento>());
        Assert.Empty(eventos.OfType<ConsecuenciaPermanenciaAplicadaEvento>());
        await using var verificacion = _fixture.CrearContexto();
        var becaPersistida = await verificacion.Becas.SingleAsync(valor => valor.Id == beca.Id);
        var reportePersistido = await verificacion.ReportesAcademicos
            .Include(valor => valor.Evaluacion)
            .SingleAsync(valor => valor.Id == reporte.Id);
        Assert.Equal(EstadoBeca.Activa, becaPersistida.Estado);
        Assert.Equal(historialInicial, await verificacion.HistorialEstadosBeca
            .CountAsync(valor => valor.BecaId == beca.Id));
        Assert.Equal(EstadoConsecuencia.Confirmada,
            reportePersistido.Evaluacion?.EstadoConfirmacion);
        Assert.Null(reportePersistido.Evaluacion?.ConsecuenciaAplicada);
    }

    [Fact]
    public async Task ReconstruyeReporteConEvaluacionYCuatroCondiciones()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var contextoOperacion = DatosPrueba.CrearContextoOperacion();
        var datos = new DatosReporteAcademico(beca.Id,
            beca.PeriodoVigenteActualId, 17m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1), 40,
            "gs://becas/servicio/constancia.pdf", new string('b', 64));
        var reporte = ReporteAcademico.Registrar(datos, contextoOperacion);
        reporte.IniciarRevision(contextoOperacion);
        reporte.Evaluar(beca.Oferta!.CondicionesSnapshot, contextoOperacion);
        reporte.DecidirConsecuencia(true, "Cumple las condiciones",
            "Continuar en el siguiente periodo", contextoOperacion);

        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            escritura.ReportesAcademicos.Add(reporte);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _fixture.CrearContexto();
        var reconstruido = await lectura.ReportesAcademicos
            .Include(valor => valor.Evaluacion!)
            .ThenInclude(valor => valor.DetalleCondiciones)
            .AsSplitQuery()
            .SingleAsync(valor => valor.Id == reporte.Id);
        Assert.Equal(40, reconstruido.HorasServicioCumplidas);
        Assert.Equal("gs://becas/servicio/constancia.pdf", reconstruido.UrlComprobanteServicio);
        Assert.NotNull(reconstruido.Evaluacion);
        Assert.True(reconstruido.Evaluacion.CumpleCondiciones);
        Assert.Equal(4, reconstruido.Evaluacion.DetalleCondiciones.Count);
        Assert.All(reconstruido.Evaluacion.DetalleCondiciones,
            detalle => Assert.True(detalle.Cumple));
        Assert.Equal(EstadoConsecuencia.Confirmada,
            reconstruido.Evaluacion.EstadoConfirmacion);

        var repositorio = new ReporteAcademicoRepository(lectura);
        var consultaReporte = await repositorio.ObtenerParaConsultaAsync(reporte.Id);
        var consultaEvaluacion = await repositorio.ObtenerPorEvaluacionParaConsultaAsync(
            reconstruido.Evaluacion.Id);
        Assert.NotNull(consultaReporte?.Evaluacion);
        Assert.Equal(4, consultaEvaluacion?.Evaluacion?.DetalleCondiciones.Count);
        Assert.True(await repositorio.ExistePorBecaYPeriodoAsync(
            beca.Id, beca.PeriodoVigenteActualId));
    }

    [Fact]
    public async Task RechazaPromedioFueraDeRangoEnPostgreSql()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var datos = new DatosReporteAcademico(beca.Id,
            beca.PeriodoVigenteActualId, 16m, 4, 0, 0, 16,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1));
        var reporte = ReporteAcademico.Registrar(datos, DatosPrueba.CrearContextoOperacion());

        await using var contexto = _fixture.CrearContexto();
        contexto.Becas.Add(beca);
        contexto.ReportesAcademicos.Add(reporte);
        await contexto.SaveChangesAsync();

        var excepcion = await Assert.ThrowsAsync<PostgresException>(() =>
            contexto.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE becas.reporte_academico SET promedio_periodo = 21 WHERE id = {reporte.Id}"));
        Assert.Equal("ck_reporte_academico_promedio", excepcion.ConstraintName);
    }
}
