using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Servicios;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.ListaEspera.Servicios;

public sealed class GeneradorListaEsperaRankingTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01969000-0000-7000-8000-000000002001");
    private static readonly Guid _programaBecaId = Guid.Parse("01969000-0000-7000-8000-000000002002");
    private static readonly Guid _coordinadorId = Guid.Parse("01969000-0000-7000-8000-000000002003");
    private static readonly DateTimeOffset _inicioRevision = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _cierreRevision = new(2026, 11, 20, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahoraCerrado = new(2026, 11, 27, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Debe_EncolarEntradasEnOrdenSecuencialSinGuardarCambiosAparte_Cuando_HayResultadosEnListaDeEspera()
    {
        var repositorio = Substitute.For<IEntradaListaEsperaRepository>();
        IReadOnlyCollection<EntradaListaEspera>? capturadas = null;
        repositorio
            .AgregarRangoAsync(Arg.Do<IReadOnlyCollection<EntradaListaEspera>>(e => capturadas = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var post1 = Guid.NewGuid();
        var post2 = Guid.NewGuid();
        var post3 = Guid.NewGuid();
        var post4 = Guid.NewGuid();

        var r1 = CrearResultadoConfirmado(post1, 95.00m, 1, 1500.00m, ClasificacionRanking.DentroDeCupo);
        var r2 = CrearResultadoConfirmado(post2, 90.00m, 2, 1400.00m, ClasificacionRanking.DentroDeCupo);
        var r3 = CrearResultadoConfirmado(post3, 85.00m, 3, 1500.00m, ClasificacionRanking.ListaDeEspera);
        var r4 = CrearResultadoConfirmado(post4, 80.00m, 4, 1200.00m, ClasificacionRanking.ListaDeEspera);

        using var cts = new CancellationTokenSource();
        var generador = new GeneradorListaEsperaRankingAdapter(repositorio);
        generador.Should().BeAssignableTo<IGeneradorListaEsperaRanking>();

        var cantidad = await generador.GenerarDesdeRankingConfirmadoAsync(
            _convocatoriaId,
            _programaBecaId,
            [r4, r1, r3, r2],
            _ahoraCerrado,
            _coordinadorId,
            cts.Token);

        cantidad.Should().Be(2);
        await repositorio.Received(1).AgregarRangoAsync(Arg.Any<IReadOnlyCollection<EntradaListaEspera>>(), cts.Token);
        capturadas.Should().NotBeNull();
        var lista = capturadas!.ToArray();
        lista.Should().HaveCount(2);
        lista[0].PostulacionId.Should().Be(post3);
        lista[0].PosicionOrden.Should().Be(1);
        lista[0].MontoEstimadoBecaSnapshot.Should().Be(1500.00m);
        lista[0].Estado.Should().Be(EstadoListaEspera.EnEspera);
        lista[1].PostulacionId.Should().Be(post4);
        lista[1].PosicionOrden.Should().Be(2);
        lista[1].MontoEstimadoBecaSnapshot.Should().Be(1200.00m);
        lista[1].Estado.Should().Be(EstadoListaEspera.EnEspera);
    }

    [Fact]
    public async Task Debe_NoInvocarAgregarRango_Cuando_TodosLosResultadosQuedanDentroDeCupo()
    {
        var repositorio = Substitute.For<IEntradaListaEsperaRepository>();
        var r1 = CrearResultadoConfirmado(Guid.NewGuid(), 95.00m, 1, 1500.00m, ClasificacionRanking.DentroDeCupo);

        var generador = new GeneradorListaEsperaRankingAdapter(repositorio);
        var cantidad = await generador.GenerarDesdeRankingConfirmadoAsync(
            _convocatoriaId,
            _programaBecaId,
            [r1],
            _ahoraCerrado,
            _coordinadorId);

        cantidad.Should().Be(0);
        await repositorio.DidNotReceiveWithAnyArgs().AgregarRangoAsync(default!, default);
    }

    [Fact]
    public async Task Debe_InvocarGeneradorListaEsperaAntesDelUnicoGuardarCambios_Cuando_SeConfirmaRanking()
    {
        var usuarioActual = Substitute.For<IUsuarioActual>();
        usuarioActual.EstaAutenticado.Returns(true);
        usuarioActual.UsuarioId.Returns(_coordinadorId);

        var rankingRepository = Substitute.For<IRankingConvocatoriaRepository>();
        var consultaEstadoEvaluaciones = Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
        var entradaRepository = Substitute.For<IEntradaListaEsperaRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var post1 = Guid.NewGuid();
        var post2 = Guid.NewGuid();
        var post3 = Guid.NewGuid();
        var r1 = CrearResultadoProvisional(post1, 95.00m);
        var r2 = CrearResultadoProvisional(post2, 85.00m);
        var r3 = CrearResultadoProvisional(post3, 75.00m);

        rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2, r3]);
        consultaEstadoEvaluaciones.ExistenEvaluacionesPendientesOSinConsolidarAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);
        convocatoriaClient.ObtenerVentanaRevisionAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                _convocatoriaId,
                EstadoConvocatoriaExterna.Publicada,
                _inicioRevision,
                _cierreRevision));
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                _convocatoriaId,
                _programaBecaId,
                cuposAsignadosConvocatoria: 1,
                cuposDisponiblesPrograma: 1,
                presupuestoDisponibleBruto: 5000.00m,
                presupuestoComprometidoRenovaciones: 0.00m,
                moneda: "USD"));
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId,
                _programaBecaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [post1] = 1500.00m,
                [post2] = 1400.00m,
                [post3] = 1200.00m
            });

        var ordenEjecucion = new List<string>();
        IReadOnlyCollection<EntradaListaEspera>? entradasCreadas = null;
        entradaRepository
            .AgregarRangoAsync(Arg.Do<IReadOnlyCollection<EntradaListaEspera>>(e =>
            {
                ordenEjecucion.Add("AgregarRango");
                entradasCreadas = e;
            }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ordenEjecucion.Add("GuardarCambios");
                return Task.FromResult(5);
            });

        var generador = new GeneradorListaEsperaRankingAdapter(entradaRepository);
        var handler = new ConfirmarRankingConvocatoriaCommandHandler(
            usuarioActual,
            rankingRepository,
            consultaEstadoEvaluaciones,
            convocatoriaClient,
            disponibilidadClient,
            proveedorMonto,
            generador,
            unitOfWork,
            new ConfirmarRankingConvocatoriaCommandValidator(),
            new RelojFijo(_ahoraCerrado));

        var dto = await handler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(_convocatoriaId));

        dto.EstadoRanking.Should().Be("CONFIRMADO");
        ordenEjecucion.Should().Equal("AgregarRango", "GuardarCambios");
        await unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
        entradasCreadas.Should().NotBeNull();
        entradasCreadas!.Select(e => (e.PostulacionId, e.PosicionOrden))
            .Should().Equal((post2, 1), (post3, 2));
    }

    private static ResultadoConsolidadoRanking CrearResultadoProvisional(Guid postulacionId, decimal puntaje)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 2.00m,
            umbralDiscrepanciaAplicado: 15.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(calculo, _inicioRevision, UsuarioSistema.Id);
    }

    private static ResultadoConsolidadoRanking CrearResultadoConfirmado(
        Guid postulacionId,
        decimal puntaje,
        int posicion,
        decimal monto,
        ClasificacionRanking clasificacion)
    {
        var resultado = CrearResultadoProvisional(postulacionId, puntaje);
        resultado.ConfirmarRanking(posicion, monto, clasificacion, _ahoraCerrado, _coordinadorId);
        return resultado;
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
