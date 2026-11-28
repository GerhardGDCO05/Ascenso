using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Comandos.PromoverCandidatoListaEspera;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ascenso.Postulaciones.Application.Tests.ListaEspera.Comandos;

public sealed class PromoverCandidatoListaEsperaCommandHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01969000-0000-7000-8000-000000006001");
    private static readonly Guid _programaBecaId = Guid.Parse("01969000-0000-7000-8000-000000006002");
    private static readonly Guid _coordinadorId = Guid.Parse("01969000-0000-7000-8000-000000006003");
    private static readonly DateTimeOffset _fechaIngreso = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora = new(2026, 11, 27, 16, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IGestorConcurrenciaListaEspera _gestorConcurrencia = Substitute.For<IGestorConcurrenciaListaEspera>();
    private readonly IResultadoConsolidadoRepository _resultadoRepository = Substitute.For<IResultadoConsolidadoRepository>();
    private readonly IDisponibilidadRankingClient _disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PromoverCandidatoListaEsperaCommandValidator _validator = new();
    private readonly TimeProvider _timeProvider = new RelojFijo(_ahora);

    public PromoverCandidatoListaEsperaCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);
    }

    private PromoverCandidatoListaEsperaCommandHandler CrearHandler()
        => new(
            _usuarioActual,
            _gestorConcurrencia,
            _resultadoRepository,
            _disponibilidadClient,
            _unitOfWork,
            _validator,
            _timeProvider);

    [Fact]
    public async Task Debe_RechazarConValidationException400_Cuando_EntradaDelComandoEsInvalida()
    {
        var handler = CrearHandler();

        var exConvocatoria = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();
        exConvocatoria.Which.Errors.Should().OnlyContain(e => e.ErrorCode == ListaEsperaInvalidaException.CodigoError);

        var exEntradaVacia = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId, Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();
        exEntradaVacia.Which.Errors.Should().OnlyContain(e => e.ErrorCode == ListaEsperaInvalidaException.CodigoError);

        var motivoLargo = new string('M', EntradaListaEspera.LongitudMaximaMotivoPromocion + 1);
        var exMotivo = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId, null, motivoLargo)))
            .Should().ThrowAsync<ValidationException>();
        exMotivo.Which.Errors.Should().OnlyContain(e => e.ErrorCode == MotivoPromocionInvalidoException.CodigoError);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConNoAutenticado401_Cuando_UsuarioNoEstaAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);
        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId)))
            .Should().ThrowAsync<NoAutenticadoException>();

        ex.Which.EstadoHttp.Should().Be(401);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_PromoverCandidatoPorIdVerificandoMsPrgYResultadoConsolidado_Cuando_CumpleFifoYDisponibilidad()
    {
        using var cts = new CancellationTokenSource();
        var postulacionId = Guid.NewGuid();
        var e1 = CrearEntrada(1, postulacionId, montoSnapshot: 1200.00m);
        e1.MarcarElegiblePorVacante(_fechaIngreso.AddHours(2), UsuarioSistema.Id);
        var e2 = CrearEntrada(2, Guid.NewGuid(), montoSnapshot: 1100.00m);
        ConfigurarBloqueo([e1, e2]);

        var resultado = CrearResultadoConfirmado(postulacionId, posicion: 3, monto: 1500.00m);
        _resultadoRepository.ObtenerPorPostulacionAsync(postulacionId, cts.Token).Returns(resultado);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns(CrearDisponibilidad(cupos: 1, presupuestoEfectivo: 2000.00m));

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(
            new PromoverCandidatoListaEsperaCommand(_convocatoriaId, e1.Id, "  Vacante confirmada  "),
            cts.Token);

        dto.HabilitadaParaAdjudicacion.Should().BeTrue();
        dto.Entrada.Id.Should().Be(e1.Id);
        dto.Entrada.Estado.Should().Be("PROMOVIDA");
        dto.Entrada.PromovidoPorUsuarioId.Should().Be(_coordinadorId);
        dto.Entrada.FechaPromocion.Should().Be(_ahora);
        dto.Entrada.FechaElegibilidad.Should().Be(_fechaIngreso.AddHours(2));
        dto.Entrada.MotivoPromocion.Should().Be("Vacante confirmada");
        dto.Entrada.Version.Should().Be(3);
        dto.DisponibilidadVerificada.CuposDisponiblesEfectivos.Should().Be(1);
        dto.DisponibilidadVerificada.PresupuestoDisponibleEfectivo.Should().Be(2000.00m);

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_PromoverSiguienteEnColaDesdeEnEspera_Cuando_EntradaListaEsperaIdEsNulo()
    {
        using var cts = new CancellationTokenSource();
        var post1 = Guid.NewGuid();
        var post2 = Guid.NewGuid();
        var e1 = CrearEntrada(1, post1, montoSnapshot: 1500.00m);
        e1.Promover(_fechaIngreso.AddHours(1), _coordinadorId);
        var e2 = CrearEntrada(2, post2, montoSnapshot: 1400.00m);
        ConfigurarBloqueo([e1, e2]);

        _resultadoRepository.ObtenerPorPostulacionAsync(post2, cts.Token)
            .Returns((ResultadoConsolidadoRanking?)null);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns(CrearDisponibilidad(cupos: 2, presupuestoEfectivo: 1400.00m));

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(
            new PromoverCandidatoListaEsperaCommand(_convocatoriaId, EntradaListaEsperaId: null, MotivoPromocion: null),
            cts.Token);

        dto.HabilitadaParaAdjudicacion.Should().BeTrue();
        dto.Entrada.Id.Should().Be(e2.Id);
        dto.Entrada.Estado.Should().Be("PROMOVIDA");
        dto.Entrada.FechaElegibilidad.Should().BeNull();
        dto.Entrada.PromovidoPorUsuarioId.Should().Be(_coordinadorId);
        dto.Entrada.FechaPromocion.Should().Be(_ahora);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_RechazarPorFifoYaPromovidaNoEncontradaOSinCandidatosSinConsultarMsPrg()
    {
        var e1 = CrearEntrada(1, Guid.NewGuid(), 1500.00m);
        var e2 = CrearEntrada(2, Guid.NewGuid(), 1400.00m);
        var e3 = CrearEntrada(3, Guid.NewGuid(), 1300.00m);
        e1.Promover(_fechaIngreso.AddHours(1), _coordinadorId);
        ConfigurarBloqueo([e1, e2, e3]);

        var handler = CrearHandler();

        var exFifo = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId, e3.Id)))
            .Should().ThrowAsync<OrdenListaEsperaVioladoException>();
        exFifo.Which.EstadoHttp.Should().Be(409);

        var exPromovida = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId, e1.Id)))
            .Should().ThrowAsync<EntradaListaEsperaYaPromovidaException>();
        exPromovida.Which.EstadoHttp.Should().Be(409);

        var exNoEncontrada = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId, Guid.NewGuid())))
            .Should().ThrowAsync<EntradaListaEsperaNoEncontradaException>();
        exNoEncontrada.Which.EstadoHttp.Should().Be(404);

        ConfigurarBloqueo([e1]);
        var exSinCandidatos = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ListaEsperaSinCandidatosException>();
        exSinCandidatos.Which.EstadoHttp.Should().Be(404);

        await _disponibilidadClient.DidNotReceiveWithAnyArgs().ObtenerDisponibilidadPorConvocatoriaAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCuandoMsPrgReportaSinCupoOPresupuestoInsuficienteUsandoMontoConsolidado()
    {
        var postId = Guid.NewGuid();
        var e1 = CrearEntrada(1, postId, montoSnapshot: 1000.00m);
        ConfigurarBloqueo([e1]);

        var resultado = CrearResultadoConfirmado(postId, posicion: 2, monto: 2000.00m);
        _resultadoRepository.ObtenerPorPostulacionAsync(postId, Arg.Any<CancellationToken>()).Returns(resultado);

        var handler = CrearHandler();

        // Sin cupos -> 409 SIN_CUPO_DISPONIBLE_PROMOCION
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(CrearDisponibilidad(cupos: 0, presupuestoEfectivo: 5000.00m));

        var exCupo = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId, e1.Id)))
            .Should().ThrowAsync<SinCupoDisponiblePromocionException>();
        exCupo.Which.EstadoHttp.Should().Be(409);
        e1.Estado.Should().Be(EstadoListaEspera.EnEspera);

        // Presupuesto menor al monto consolidado (1500 < 2000) -> 422 PRESUPUESTO_INSUFICIENTE_PROMOCION
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(CrearDisponibilidad(cupos: 1, presupuestoEfectivo: 1500.00m));

        var exPresupuesto = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId, e1.Id)))
            .Should().ThrowAsync<PresupuestoInsuficientePromocionException>();
        exPresupuesto.Which.EstadoHttp.Should().Be(422);
        e1.Estado.Should().Be(EstadoListaEspera.EnEspera);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_FallarCerrado_Cuando_MsPrgNoEncuentraConvocatoriaProgramaOEstaIndisponible()
    {
        var postId = Guid.NewGuid();
        var e1 = CrearEntrada(1, postId, montoSnapshot: 1500.00m);
        ConfigurarBloqueo([e1]);
        var handler = CrearHandler();

        // Convocatoria no encontrada (null -> 404 CONVOCATORIA_NO_ENCONTRADA)
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((DisponibilidadProgramaRanking?)null);

        var exConvocatoria = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ConvocatoriaNoEncontradaException>();
        exConvocatoria.Which.EstadoHttp.Should().Be(404);

        // Programa no encontrado (404 PROGRAMA_NO_ENCONTRADO)
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProgramaBecaNoEncontradoException());

        var exPrograma = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ProgramaBecaNoEncontradoException>();
        exPrograma.Which.EstadoHttp.Should().Be(404);

        // Dependencia no disponible (503 DEPENDENCIA_NO_DISPONIBLE)
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependenciaNoDisponibleException("MS-PRG no respondió a tiempo."));

        var exDependencia = await handler
            .Invoking(h => h.HandleAsync(new PromoverCandidatoListaEsperaCommand(_convocatoriaId)))
            .Should().ThrowAsync<DependenciaNoDisponibleException>();
        exDependencia.Which.EstadoHttp.Should().Be(503);

        e1.Estado.Should().Be(EstadoListaEspera.EnEspera);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private void ConfigurarBloqueo(IReadOnlyList<EntradaListaEspera> entradas)
    {
        _gestorConcurrencia
            .EjecutarConBloqueoConvocatoriaAsync(
                _convocatoriaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<PromocionListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var accion = callInfo.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<PromocionListaEsperaDto>>>(1);
                var token = callInfo.ArgAt<CancellationToken>(2);
                return accion(entradas, token);
            });
    }

    private static EntradaListaEspera CrearEntrada(int posicion, Guid postulacionId, decimal montoSnapshot)
        => EntradaListaEspera.Crear(
            _convocatoriaId,
            _programaBecaId,
            postulacionId,
            posicion,
            montoSnapshot,
            _fechaIngreso,
            _coordinadorId);

    private static ResultadoConsolidadoRanking CrearResultadoConfirmado(Guid postulacionId, int posicion, decimal monto)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 82.00m,
            diferenciaEntreEvaluadores: 2.00m,
            umbralDiscrepanciaAplicado: 15.00m,
            alertaDiscrepancia: false);

        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaIngreso, UsuarioSistema.Id);
        resultado.ConfirmarRanking(posicion, monto, ClasificacionRanking.ListaDeEspera, _fechaIngreso, _coordinadorId);
        return resultado;
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(int cupos, decimal presupuestoEfectivo)
        => new(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: Math.Max(cupos, 1),
            cuposDisponiblesPrograma: cupos,
            presupuestoDisponibleBruto: presupuestoEfectivo,
            presupuestoComprometidoRenovaciones: 0.00m,
            moneda: "USD");

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
