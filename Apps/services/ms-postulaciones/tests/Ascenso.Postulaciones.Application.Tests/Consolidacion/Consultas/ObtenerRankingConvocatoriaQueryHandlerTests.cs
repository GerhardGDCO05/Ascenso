using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Consultas;

public sealed class ObtenerRankingConvocatoriaQueryHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000007001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000007002");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000007003");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000007004");
    private static readonly Guid _postulacion3Id = Guid.Parse("01968000-0000-7000-8000-000000007005");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000007006");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly IRankingConvocatoriaRepository _rankingRepository = Substitute.For<IRankingConvocatoriaRepository>();
    private readonly IDisponibilidadRankingClient _disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
    private readonly ObtenerRankingConvocatoriaQueryValidator _validator = new();

    private ObtenerRankingConvocatoriaQueryHandler CrearHandler()
        => new(_rankingRepository, _disponibilidadClient, _proveedorMonto, _validator);

    [Theory]
    [InlineData(true, null, 1, 20)]
    [InlineData(false, null, 0, 20)]
    [InlineData(false, null, 1, 0)]
    [InlineData(false, null, 1, 101)]
    [InlineData(false, "ADJUDICADO", 1, 20)]
    public async Task Debe_RechazarConValidationException_Cuando_ParametrosDeConsultaSonInvalidos(
        bool convocatoriaVacia,
        string? clasificacion,
        int pagina,
        int tamano)
    {
        var handler = CrearHandler();
        var query = new ObtenerRankingConvocatoriaQuery(
            convocatoriaVacia ? Guid.Empty : _convocatoriaId,
            clasificacion,
            pagina,
            tamano);

        var accion = () => handler.HandleAsync(query);

        var ex = await accion.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().OnlyContain(e => e.ErrorCode == RankingInvalidoException.CodigoError);
        await _rankingRepository.DidNotReceiveWithAnyArgs()
            .ListarPorConvocatoriaAsync(default, default);
    }

    [Fact]
    public async Task Debe_ProyectarEnVivoConEmpatesAdvertenciasFiltroYPaginacion_Cuando_RankingEsProvisional()
    {
        using var cts = new CancellationTokenSource();
        var r1 = CrearResultado(_postulacion1Id, 95.00m, _fechaBase, alerta: true);
        var r2 = CrearResultado(_postulacion2Id, 88.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(_postulacion3Id, 88.00m, _fechaBase.AddMinutes(2));

        var disponibilidad = CrearDisponibilidad(cuposAsignados: 2, cuposPrograma: 2, presupuestoBruto: 10000.00m, renovaciones: 1000.00m);

        _rankingRepository.ListarPorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns([r1, r2, r3]);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns(disponibilidad);
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId,
                _programaBecaId,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 3),
                cts.Token)
            .Returns(new Dictionary<Guid, decimal>
            {
                [_postulacion1Id] = 1500.00m,
                [_postulacion2Id] = 1500.00m,
                [_postulacion3Id] = 1500.00m
            });

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(
            new ObtenerRankingConvocatoriaQuery(_convocatoriaId, "DENTRO_DE_CUPO", Pagina: 1, Tamano: 1),
            cts.Token);

        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.EstadoRanking.Should().Be("PROVISIONAL");
        dto.Disponibilidad.Should().NotBeNull();
        dto.Disponibilidad!.PresupuestoDisponibleEfectivo.Should().Be(9000.00m);
        dto.TotalPostulantes.Should().Be(3);
        dto.TotalDentroDeCupo.Should().Be(2);
        dto.TotalListaDeEspera.Should().Be(1);
        dto.TieneEmpateEnLineaDeCorte.Should().BeTrue();
        dto.PuntajeEmpateEnLineaDeCorte.Should().Be(88.00m);
        dto.PostulacionesEmpatadasEnCorte.Should().Equal(_postulacion2Id, _postulacion3Id);
        dto.TieneAdvertenciaDiscrepanciasEnCorte.Should().BeTrue();
        dto.CantidadDiscrepanciasSinRevisarEnCorte.Should().Be(1);
        dto.PostulacionesConDiscrepanciaSinRevisarEnCorte.Should().Equal(_postulacion1Id);
        dto.Elementos.Total.Should().Be(2);
        dto.Elementos.Items.Should().ContainSingle();
        dto.Elementos.Items[0].PostulacionId.Should().Be(_postulacion1Id);
        dto.Elementos.Items[0].Clasificacion.Should().Be("DENTRO_DE_CUPO");
    }

    [Fact]
    public async Task Debe_RetornarRankingProvisionalVacioConDisponibilidad_Cuando_NoHayResultadosConsolidados()
    {
        var disponibilidad = CrearDisponibilidad(cuposAsignados: 3, cuposPrograma: 2, presupuestoBruto: 6000.00m);
        _rankingRepository.ListarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(disponibilidad);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ObtenerRankingConvocatoriaQuery(_convocatoriaId));

        dto.EstadoRanking.Should().Be("PROVISIONAL");
        dto.Disponibilidad.Should().NotBeNull();
        dto.Disponibilidad!.CuposDisponiblesEfectivos.Should().Be(2);
        dto.TotalPostulantes.Should().Be(0);
        dto.Elementos.Items.Should().BeEmpty();
        dto.Elementos.Total.Should().Be(0);

        await _proveedorMonto.DidNotReceiveWithAnyArgs()
            .ObtenerMontosEstimadosPorPostulacionAsync(default, default, default!, default);
    }

    [Fact]
    public async Task Debe_DevolverFotografiaInmutableSinInvocarMsPrg_Cuando_RankingEstaConfirmado()
    {
        using var cts = new CancellationTokenSource();
        var fechaConf = _fechaBase.AddHours(6);
        var r1 = CrearResultado(_postulacion1Id, 95.00m, _fechaBase);
        r1.ConfirmarRanking(1, 1500.00m, ClasificacionRanking.DentroDeCupo, fechaConf, _coordinadorId);

        var r2 = CrearResultado(_postulacion2Id, 82.00m, _fechaBase.AddMinutes(5));
        r2.ConfirmarRanking(2, 1200.00m, ClasificacionRanking.ListaDeEspera, fechaConf, _coordinadorId);

        _rankingRepository.ListarPorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns([r1, r2]);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(
            new ObtenerRankingConvocatoriaQuery(_convocatoriaId, "LISTA_DE_ESPERA", Pagina: 1, Tamano: 10),
            cts.Token);

        dto.EstadoRanking.Should().Be("CONFIRMADO");
        dto.FechaConfirmacionRanking.Should().Be(fechaConf);
        dto.ConfirmadoPorUsuarioId.Should().Be(_coordinadorId);
        dto.Disponibilidad.Should().BeNull();
        dto.TotalPostulantes.Should().Be(2);
        dto.TotalDentroDeCupo.Should().Be(1);
        dto.TotalListaDeEspera.Should().Be(1);
        dto.MontoTotalDentroDeCupo.Should().Be(1500.00m);
        dto.Elementos.Items.Should().ContainSingle();
        dto.Elementos.Items[0].PostulacionId.Should().Be(_postulacion2Id);
        dto.Elementos.Items[0].Clasificacion.Should().Be("LISTA_DE_ESPERA");

        await _disponibilidadClient.DidNotReceiveWithAnyArgs()
            .ObtenerDisponibilidadPorConvocatoriaAsync(default, default);
        await _proveedorMonto.DidNotReceiveWithAnyArgs()
            .ObtenerMontosEstimadosPorPostulacionAsync(default, default, default!, default);
    }

    [Fact]
    public async Task Debe_LanzarConvocatoriaNoEncontrada_Cuando_DisponibilidadRetornaNullEnProvisional()
    {
        _rankingRepository.ListarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((DisponibilidadProgramaRanking?)null);

        var handler = CrearHandler();
        var accion = () => handler.HandleAsync(new ObtenerRankingConvocatoriaQuery(_convocatoriaId));

        await accion.Should().ThrowAsync<ConvocatoriaNoEncontradaException>();
    }

    [Fact]
    public async Task Debe_PropagarProgramaNoEncontradoYDependenciaNoDisponible_Cuando_MsPrgFalla()
    {
        _rankingRepository.ListarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([]);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProgramaBecaNoEncontradoException());

        var handler = CrearHandler();
        await handler.Invoking(h => h.HandleAsync(new ObtenerRankingConvocatoriaQuery(_convocatoriaId)))
            .Should().ThrowAsync<ProgramaBecaNoEncontradoException>();

        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependenciaNoDisponibleException());

        await handler.Invoking(h => h.HandleAsync(new ObtenerRankingConvocatoriaQuery(_convocatoriaId)))
            .Should().ThrowAsync<DependenciaNoDisponibleException>();
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(
        int cuposAsignados,
        int cuposPrograma,
        decimal presupuestoBruto,
        decimal renovaciones = 0.00m)
        => new(_convocatoriaId, _programaBecaId, cuposAsignados, cuposPrograma, presupuestoBruto, renovaciones, "USD");

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        DateTimeOffset fecha,
        bool alerta = false)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: alerta ? 24.00m : 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);

        return ResultadoConsolidadoRanking.Crear(calculo, fecha, UsuarioSistema.Id);
    }
}
