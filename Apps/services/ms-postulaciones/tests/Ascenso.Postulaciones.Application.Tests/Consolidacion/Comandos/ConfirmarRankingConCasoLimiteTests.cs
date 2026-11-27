using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Comandos;

public sealed class ConfirmarRankingConCasoLimiteTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000011001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000011002");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000011003");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000011004");
    private static readonly Guid _postulacion3Id = Guid.Parse("01968000-0000-7000-8000-000000011005");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000011006");
    private static readonly DateTimeOffset _inicioRevision = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _cierreRevision = new(2026, 11, 20, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahoraCerrado = new(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRankingConvocatoriaRepository _rankingRepository = Substitute.For<IRankingConvocatoriaRepository>();
    private readonly IConsultaEstadoEvaluacionesConvocatoriaRepository _consultaEstadoEvaluaciones =
        Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
    private readonly IDisponibilidadRankingClient _disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfirmarRankingConvocatoriaCommandValidator _validator = new();
    private readonly TimeProvider _timeProvider;

    public ConfirmarRankingConCasoLimiteTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);
        _timeProvider = new RelojFijo(_ahoraCerrado);
    }

    private ConfirmarRankingConvocatoriaCommandHandler CrearHandler()
        => new(
            _usuarioActual,
            _rankingRepository,
            _consultaEstadoEvaluaciones,
            _convocatoriaClient,
            _disponibilidadClient,
            _proveedorMonto,
            _unitOfWork,
            _validator,
            _timeProvider);

    [Fact]
    public async Task Debe_DesbloquearConfirmacionYPreservarMetadatosDeCasoLimite_Cuando_EmpateFueResueltoValidamente()
    {
        var r1 = CrearResultado(_postulacion1Id, 95.00m);
        var r2 = CrearResultado(_postulacion2Id, 88.00m, minutosDesfase: 1);
        var r3 = CrearResultado(_postulacion3Id, 88.00m, minutosDesfase: 2);

        var disponibilidad = CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 1000.00m,
            [_postulacion2Id] = 1000.00m,
            [_postulacion3Id] = 1000.00m
        };

        var corteInicial = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);
        const string justificacion = "Desempate resuelto seleccionando postulante 3 por vulnerabilidad.";
        ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, r2, r3],
            [_postulacion3Id],
            justificacion,
            _ahoraCerrado.AddMinutes(-30),
            _coordinadorId);

        ConfigurarPrecondiciones([r1, r2, r3], disponibilidad, montos);

        var handler = CrearHandler();
        var resultado = await handler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId));

        resultado.EstadoRanking.Should().Be("CONFIRMADO");
        resultado.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        resultado.TieneCasoLimiteResuelto.Should().BeTrue();
        resultado.TotalDentroDeCupo.Should().Be(2);
        resultado.TotalListaDeEspera.Should().Be(1);

        r3.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Confirmado);
        r3.PosicionRanking.Should().Be(2);
        r3.Clasificacion.Should().Be(ClasificacionRanking.DentroDeCupo);
        r3.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
        r3.SeleccionadoEnCasoLimite.Should().BeTrue();
        r3.JustificacionDesempateCasoLimite.Should().Be(justificacion);

        r2.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Confirmado);
        r2.PosicionRanking.Should().Be(3);
        r2.Clasificacion.Should().Be(ClasificacionRanking.ListaDeEspera);
        r2.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
        r2.SeleccionadoEnCasoLimite.Should().BeFalse();

        resultado.Elementos.Items[1].PostulacionId.Should().Be(_postulacion3Id);
        resultado.Elementos.Items[1].CasoLimiteResuelto.Should().BeTrue();
        resultado.Elementos.Items[1].TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");

        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RebloquearCon422EmpatesSinResolver_Cuando_DisponibilidadBajaTrasDesempateResuelto()
    {
        var r1 = CrearResultado(_postulacion1Id, 88.00m);
        var r2 = CrearResultado(_postulacion2Id, 88.00m, minutosDesfase: 1);
        var r3 = CrearResultado(_postulacion3Id, 88.00m, minutosDesfase: 2);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 1000.00m,
            [_postulacion2Id] = 1000.00m,
            [_postulacion3Id] = 1000.00m
        };

        var corteInicial = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [r1, r2, r3],
            montos,
            CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m));

        ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, r2, r3],
            [_postulacion1Id, _postulacion2Id],
            "Se seleccionan dos de los tres empatados con cupo inicial de 2.",
            _ahoraCerrado.AddMinutes(-20),
            _coordinadorId);

        // Antes de confirmar, MS-PRG reduce cupos a 1 -> selección de 2 ya no cabe
        ConfigurarPrecondiciones([r1, r2, r3], CrearDisponibilidad(cupos: 1, presupuesto: 10000.00m), montos);

        var handler = CrearHandler();
        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<EmpatesSinResolverEnCorteException>();

        ex.Which.Codigo.Should().Be("EMPATES_SIN_RESOLVER");
        ex.Which.EstadoHttp.Should().Be(422);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RebloquearCon422DecisionCasoLimiteExcedeDisponibilidad_Cuando_PresupuestoBajaTrasCasoLimitePresupuestario()
    {
        var r1 = CrearResultado(_postulacion1Id, 94.00m);
        var r2 = CrearResultado(_postulacion2Id, 89.00m, minutosDesfase: 1);
        var r3 = CrearResultado(_postulacion3Id, 80.00m, minutosDesfase: 2);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 2000.00m,
            [_postulacion2Id] = 2000.00m,
            [_postulacion3Id] = 400.00m
        };

        var corteInicial = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [r1, r2, r3],
            montos,
            CrearDisponibilidad(cupos: 5, presupuesto: 2500.00m));

        ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, r2, r3],
            [_postulacion3Id],
            "Asignación de saldo remanente a postulante 3 por $400.",
            _ahoraCerrado.AddMinutes(-20),
            _coordinadorId);

        // Antes de confirmar, MS-PRG reduce presupuesto a 2200 -> remanente 200 < 400
        ConfigurarPrecondiciones([r1, r2, r3], CrearDisponibilidad(cupos: 5, presupuesto: 2200.00m), montos);

        var handler = CrearHandler();
        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId)))
            .Should().ThrowAsync<DecisionCasoLimiteInvalidaPorDisponibilidadException>();

        ex.Which.Codigo.Should().Be("DECISION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD");
        ex.Which.EstadoHttp.Should().Be(422);
        r1.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
        r3.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private void ConfigurarPrecondiciones(
        IReadOnlyList<ResultadoConsolidadoRanking> resultados,
        DisponibilidadProgramaRanking disponibilidad,
        IReadOnlyDictionary<Guid, decimal> montos)
    {
        _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(resultados);
        _consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);
        _convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(_convocatoriaId, EstadoConvocatoriaExterna.Cerrada, _inicioRevision, _cierreRevision));
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(disponibilidad);
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId,
                disponibilidad.ProgramaBecaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(montos);
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(int cupos, decimal presupuesto)
        => new(_convocatoriaId, _programaBecaId, cupos, cupos, presupuesto, 0.00m, "USD");

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        int minutosDesfase = 0)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(
            calculo,
            _cierreRevision.AddHours(-2).AddMinutes(minutosDesfase),
            UsuarioSistema.Id);
    }

    private sealed class RelojFijo(DateTimeOffset ahoraUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahoraUtc;
    }
}
