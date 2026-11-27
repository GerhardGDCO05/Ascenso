using System.Reflection;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesCoevaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Consultas;

public sealed class ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandlerTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968200-0000-7000-8000-000000000701");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968200-0000-7000-8000-000000000702");
    private static readonly Guid _jefaturaId = Guid.Parse("01968200-0000-7000-8000-000000000703");
    private static readonly Guid _coordinadorId = Guid.Parse("01968200-0000-7000-8000-000000000704");
    private static readonly Guid _evaluadorAId = Guid.Parse("01968200-0000-7000-8000-000000000705");
    private static readonly Guid _evaluadorBId = Guid.Parse("01968200-0000-7000-8000-000000000706");
    private static readonly Guid _evaluadorCId = Guid.Parse("01968200-0000-7000-8000-000000000707");
    private static readonly Guid _evaluadorDId = Guid.Parse("01968200-0000-7000-8000-000000000708");
    private static readonly Guid _postulacionId = Guid.Parse("01968200-0000-7000-8000-000000000709");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _inicioVentana =
        new(2026, 11, 20, 4, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _cierreVentana =
        new(2026, 11, 30, 3, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahoraPostCierre =
        _cierreVentana.AddSeconds(1);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IConsultaEvaluacionesCoevaluadoresRepository _coevaluadoresRepository =
        Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>();
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient =
        Substitute.For<IConvocatoriaEvaluacionClient>();

    public ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorAId);
    }

    [Fact]
    public void Debe_ValidarCorrectamentePostulacionId_Cuando_SeEjecutaElValidador()
    {
        var validador = new ObtenerEvaluacionesCoevaluadoresPostulacionQueryValidator();

        var resultadoInvalido = validador.Validate(new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(Guid.Empty));
        var resultadoValido = validador.Validate(new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId));

        resultadoInvalido.IsValid.Should().BeFalse();
        resultadoInvalido.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(ObtenerEvaluacionesCoevaluadoresPostulacionQuery.PostulacionId)
            && e.ErrorCode == "EVALUACION_INVALIDA");
        resultadoValido.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_RechazarConValidationExceptionSinConsultarRepositorios_Cuando_PostulacionIdEsVacio()
    {
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(Guid.Empty),
            CancellationToken.None);

        var excepcion = (await accion.Should().ThrowAsync<ValidationException>()).Which;
        excepcion.Errors.Should().ContainSingle(e => e.ErrorCode == "EVALUACION_INVALIDA");

        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ObtenerAsignacionVigenteEvaluadorAsync(default, default, default);
        await _convocatoriaClient.DidNotReceiveWithAnyArgs()
            .ObtenerVentanaRevisionAsync(default, default);
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Publicada)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada)]
    public async Task Debe_RetornarSoloDefinitivasDeOtrosEvaluadoresYPropagarToken_Cuando_PeriodoRevisionEstaCerrado(
        EstadoConvocatoriaExterna estadoConvocatoria)
    {
        using var cts = new CancellationTokenSource();
        var (asignacionA, propiaDefinitiva, definitivaB, borradorC, invalidadaD) = CrearEscenarioCompleto();

        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, cts.Token)
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacionA));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, cts.Token)
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, estadoConvocatoria, _inicioVentana, _cierreVentana)));
        _coevaluadoresRepository
            .ListarDefinitivasDeCoevaluadoresAsync(_postulacionId, _evaluadorAId, cts.Token)
            .Returns(Task.FromResult<IReadOnlyList<Evaluacion>>(
                [propiaDefinitiva, definitivaB, borradorC, invalidadaD]));

        var handler = CrearHandler(_ahoraPostCierre);

        var resultado = await handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            cts.Token);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        resultado.FechaCierreRevision.Should().Be(_cierreVentana);
        resultado.Evaluaciones.Should().ContainSingle();

        var coevaluador = resultado.Evaluaciones[0];
        coevaluador.EvaluacionId.Should().Be(definitivaB.Id);
        coevaluador.RubricaEvaluacionId.Should().Be(definitivaB.RubricaEvaluacionId);
        coevaluador.Estado.Should().Be("ENVIADA_DEFINITIVA");
        coevaluador.FechaEnvio.Should().Be(definitivaB.FechaEnvio!.Value);
        coevaluador.PuntajeTotalPonderado.Should().Be(definitivaB.PuntajeTotalPonderado!.Value);
        coevaluador.ComentariosGenerales.Should().Be("Evaluación de B");
        coevaluador.Calificaciones.Should().ContainSingle();

        await _coevaluadoresRepository.Received(1)
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, cts.Token);
        await _convocatoriaClient.Received(1)
            .ObtenerVentanaRevisionAsync(_convocatoriaId, cts.Token);
        await _coevaluadoresRepository.Received(1)
            .ListarDefinitivasDeCoevaluadoresAsync(_postulacionId, _evaluadorAId, cts.Token);
    }

    [Fact]
    public async Task Debe_RetornarListaVaciaConFechaCierre_Cuando_NoExistenDefinitivasDeCoevaluadoresTrasCierre()
    {
        var (asignacionA, _, _, _, _) = CrearEscenarioCompleto();

        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacionA));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cerrada, _inicioVentana, _cierreVentana)));
        _coevaluadoresRepository
            .ListarDefinitivasDeCoevaluadoresAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Evaluacion>>([]));

        var handler = CrearHandler(_ahoraPostCierre);

        var resultado = await handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        resultado.FechaCierreRevision.Should().Be(_cierreVentana);
        resultado.Evaluaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RechazarCon403ConfidencialSinInvocarMsPrg_Cuando_EvaluadorNoTieneAsignacionVigente()
    {
        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(null));
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionConfidencialException>()
            .Where(ex => ex.Codigo == "EVALUACION_CONFIDENCIAL" && ex.EstadoHttp == 403);

        await _convocatoriaClient.DidNotReceiveWithAnyArgs()
            .ObtenerVentanaRevisionAsync(default, default);
        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ListarDefinitivasDeCoevaluadoresAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon403ConfidencialSinInvocarMsPrg_Cuando_AsignacionFueReasignadaPorConflicto()
    {
        var (asignacionA, _, _, _, _) = CrearEscenarioCompleto();
        FijarEstadoAsignacion(asignacionA, EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacionA));
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<EvaluacionConfidencialException>()
            .Where(ex => ex.Codigo == "EVALUACION_CONFIDENCIAL" && ex.EstadoHttp == 403);

        await _convocatoriaClient.DidNotReceiveWithAnyArgs()
            .ObtenerVentanaRevisionAsync(default, default);
        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ListarDefinitivasDeCoevaluadoresAsync(default, default, default);
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 3)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, -1)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 1)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 2)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, -1)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 1)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 2)]
    public async Task Debe_RechazarCon409PeriodoRevisionAbiertoSinListarEvaluaciones_Cuando_PeriodoNoHaCerrado(
        EstadoConvocatoriaExterna estadoConvocatoria,
        int puntoTemporal)
    {
        var (asignacionA, _, _, _, _) = CrearEscenarioCompleto();
        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacionA));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, estadoConvocatoria, _inicioVentana, _cierreVentana)));

        var ahora = puntoTemporal switch
        {
            -1 => _inicioVentana.AddTicks(-1),
            1 => _inicioVentana.AddDays(3),
            2 => _cierreVentana,
            3 => _cierreVentana.AddDays(1),
            _ => throw new ArgumentOutOfRangeException(nameof(puntoTemporal))
        };
        var handler = CrearHandler(ahora);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PeriodoRevisionAbiertoException>()
            .Where(ex => ex.Codigo == "PERIODO_REVISION_ABIERTO" && ex.EstadoHttp == 409);

        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ListarDefinitivasDeCoevaluadoresAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon409ConvocatoriaCanceladaSinListarEvaluaciones_Cuando_ConvocatoriaEstaCancelada()
    {
        var (asignacionA, _, _, _, _) = CrearEscenarioCompleto();
        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacionA));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cancelada, _inicioVentana, _cierreVentana)));
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ConvocatoriaCanceladaException>()
            .Where(ex => ex.Codigo == "CONVOCATORIA_CANCELADA" && ex.EstadoHttp == 409);

        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ListarDefinitivasDeCoevaluadoresAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon404ConvocatoriaNoEncontrada_Cuando_ClienteRetornaNulo()
    {
        var (asignacionA, _, _, _, _) = CrearEscenarioCompleto();
        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacionA));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VentanaRevisionConvocatoria?>(null));
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ConvocatoriaNoEncontradaException>()
            .Where(ex => ex.Codigo == "CONVOCATORIA_NO_ENCONTRADA" && ex.EstadoHttp == 404);

        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ListarDefinitivasDeCoevaluadoresAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_FallarCerradoCon503DependenciaNoDisponible_Cuando_MsPrgNoResponde()
    {
        var (asignacionA, _, _, _, _) = CrearEscenarioCompleto();
        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(_postulacionId, _evaluadorAId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AsignacionEvaluacion?>(asignacionA));
        _convocatoriaClient
            .ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns<Task<VentanaRevisionConvocatoria?>>(_ => throw new DependenciaNoDisponibleException());
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<DependenciaNoDisponibleException>()
            .Where(ex => ex.Codigo == "DEPENDENCIA_NO_DISPONIBLE" && ex.EstadoHttp == 503);

        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ListarDefinitivasDeCoevaluadoresAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401NoAutenticado_Cuando_UsuarioNoEstaAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);

        await _coevaluadoresRepository.DidNotReceiveWithAnyArgs()
            .ObtenerAsignacionVigenteEvaluadorAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_PropagarCancelacion_Cuando_TokenEstaCancelado()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _coevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(
                _postulacionId,
                _evaluadorAId,
                Arg.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .Returns<Task<AsignacionEvaluacion?>>(_ => throw new OperationCanceledException(cts.Token));
        var handler = CrearHandler(_ahoraPostCierre);

        var accion = () => handler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(_postulacionId),
            cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    private ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler CrearHandler(DateTimeOffset ahora)
        => new(
            _usuarioActual,
            _coevaluadoresRepository,
            _convocatoriaClient,
            new ObtenerEvaluacionesCoevaluadoresPostulacionQueryValidator(),
            new RelojFijo(ahora));

    private static (
        AsignacionEvaluacion AsignacionA,
        Evaluacion PropiaDefinitiva,
        Evaluacion DefinitivaB,
        Evaluacion BorradorC,
        Evaluacion InvalidadaD) CrearEscenarioCompleto()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);

        var mA = comite.AgregarMiembro(_evaluadorAId, TipoEvaluador.Externo, _fechaBase.AddDays(1), _coordinadorId);
        var mB = comite.AgregarMiembro(_evaluadorBId, TipoEvaluador.Interno, _fechaBase.AddDays(1), _coordinadorId);
        var mC = comite.AgregarMiembro(_evaluadorCId, TipoEvaluador.Externo, _fechaBase.AddDays(1), _coordinadorId);
        var mD = comite.AgregarMiembro(_evaluadorDId, TipoEvaluador.Interno, _fechaBase.AddDays(1), _coordinadorId);

        var aA = AsignacionEvaluacion.Crear(comite, mA.Id, _postulacionId, _fechaBase.AddDays(2), _cierreVentana, _coordinadorId);
        var aB = AsignacionEvaluacion.Crear(comite, mB.Id, _postulacionId, _fechaBase.AddDays(2), _cierreVentana, _coordinadorId);
        var aC = AsignacionEvaluacion.Crear(comite, mC.Id, _postulacionId, _fechaBase.AddDays(2), _cierreVentana, _coordinadorId);
        var aD = AsignacionEvaluacion.Crear(comite, mD.Id, _postulacionId, _fechaBase.AddDays(2), _cierreVentana, _coordinadorId);

        var rubrica = RubricaEvaluacion.Crear(_programaBecaId, _convocatoriaId, "Rúbrica", _fechaBase, _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc", 100.00m, 0.00m, 20.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);

        var propia = Evaluacion.CrearBorrador(aA, rubrica, [new DatosCalificacionCriterio(c1.Id, 18.00m, "A")], "Evaluación de A", _inicioVentana.AddDays(1), _evaluadorAId);
        propia.EnviarDefinitiva(rubrica, aA.FechaLimiteEvaluacion, _inicioVentana.AddDays(2), _evaluadorAId);

        var definitivaB = Evaluacion.CrearBorrador(aB, rubrica, [new DatosCalificacionCriterio(c1.Id, 16.00m, "B")], "Evaluación de B", _inicioVentana.AddDays(1), _evaluadorBId);
        definitivaB.EnviarDefinitiva(rubrica, aB.FechaLimiteEvaluacion, _inicioVentana.AddDays(2), _evaluadorBId);

        var borradorC = Evaluacion.CrearBorrador(aC, rubrica, [new DatosCalificacionCriterio(c1.Id, 14.00m, "C")], "Borrador de C", _inicioVentana.AddDays(1), _evaluadorCId);

        var invalidadaD = Evaluacion.CrearBorrador(aD, rubrica, [new DatosCalificacionCriterio(c1.Id, 15.00m, "D")], "Invalidada de D", _inicioVentana.AddDays(1), _evaluadorDId);
        invalidadaD.EnviarDefinitiva(rubrica, aD.FechaLimiteEvaluacion, _inicioVentana.AddDays(2), _evaluadorDId);
        FijarEstadoEvaluacion(invalidadaD, EstadoEvaluacion.InvalidadaPorConflicto);

        return (aA, propia, definitivaB, borradorC, invalidadaD);
    }

    private static void FijarEstadoAsignacion(AsignacionEvaluacion asignacion, EstadoAsignacionEvaluacion estado)
    {
        var propiedad = typeof(AsignacionEvaluacion)
            .GetProperty(nameof(AsignacionEvaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);
        propiedad.Should().NotBeNull();
        propiedad!.SetValue(asignacion, estado);
    }

    private static void FijarEstadoEvaluacion(Evaluacion evaluacion, EstadoEvaluacion estado)
    {
        var propiedad = typeof(Evaluacion)
            .GetProperty(nameof(Evaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);
        propiedad.Should().NotBeNull();
        propiedad!.SetValue(evaluacion, estado);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
