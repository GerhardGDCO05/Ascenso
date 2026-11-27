using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Consultas;

public sealed class ConsultarAdjudicabilidadPostulacionQueryHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000009001");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000009002");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000009003");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly IResultadoConsolidadoRepository _resultadoRepository = Substitute.For<IResultadoConsolidadoRepository>();
    private readonly IEntradaListaEsperaRepository _entradaListaEsperaRepository = Substitute.For<IEntradaListaEsperaRepository>();
    private readonly ConsultarAdjudicabilidadPostulacionQueryValidator _validator = new();

    private ConsultarAdjudicabilidadPostulacionQueryHandler CrearHandler()
        => new(_resultadoRepository, _entradaListaEsperaRepository, _validator);

    [Fact]
    public async Task Debe_RechazarConValidationException_Cuando_PostulacionIdEsVacio()
    {
        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().OnlyContain(e => e.ErrorCode == RankingInvalidoException.CodigoError);
        await _resultadoRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorPostulacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_RetornarAdjudicableTrue_Cuando_ResultadoEstaConfirmadoDentroDeCupo()
    {
        using var cts = new CancellationTokenSource();
        var fechaConf = _fechaBase.AddHours(2);
        var resultado = CrearResultado(94.00m);
        resultado.ConfirmarRanking(1, 1500.00m, ClasificacionRanking.DentroDeCupo, fechaConf, _coordinadorId);

        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, cts.Token)
            .Returns(resultado);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId), cts.Token);

        dto.PostulacionId.Should().Be(_postulacionId);
        dto.ConvocatoriaOrigenId.Should().Be(_convocatoriaId);
        dto.Adjudicable.Should().BeTrue();
        dto.MotivoNoAdjudicable.Should().BeNull();
        dto.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dto.Clasificacion.Should().Be("DENTRO_DE_CUPO");
        dto.PosicionRanking.Should().Be(1);
        dto.MontoEstimadoBeca.Should().Be(1500.00m);
        dto.FechaConfirmacionRanking.Should().Be(fechaConf);
    }

    [Fact]
    public async Task Debe_RetornarAdjudicableFalseConRankingNoConfirmado_Cuando_ResultadoEsProvisional()
    {
        var resultado = CrearResultado(92.00m);
        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultado);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId));

        dto.Adjudicable.Should().BeFalse();
        dto.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoRankingNoConfirmado);
        dto.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        dto.Clasificacion.Should().BeNull();
        dto.PosicionRanking.Should().BeNull();
        dto.MontoEstimadoBeca.Should().BeNull();
        dto.FechaConfirmacionRanking.Should().BeNull();
    }

    [Fact]
    public async Task Debe_RetornarAdjudicableFalseConFueraDeCupo_Cuando_ResultadoEstaConfirmadoEnListaDeEspera()
    {
        var fechaConf = _fechaBase.AddHours(2);
        var resultado = CrearResultado(78.00m);
        resultado.ConfirmarRanking(5, 1200.00m, ClasificacionRanking.ListaDeEspera, fechaConf, _coordinadorId);

        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultado);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId));

        dto.Adjudicable.Should().BeFalse();
        dto.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoFueraDeCupoEnListaDeEspera);
        dto.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dto.Clasificacion.Should().Be("LISTA_DE_ESPERA");
        dto.PosicionRanking.Should().Be(5);
        dto.MontoEstimadoBeca.Should().Be(1200.00m);
        dto.FechaConfirmacionRanking.Should().Be(fechaConf);
    }

    [Fact]
    public async Task Debe_LanzarResultadoConsolidadoNoEncontrado_Cuando_PostulacionNoTieneResultado()
    {
        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns((ResultadoConsolidadoRanking?)null);

        var handler = CrearHandler();

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId)))
            .Should().ThrowAsync<ResultadoConsolidadoNoEncontradoException>();

        ex.Which.Codigo.Should().Be("RESULTADO_CONSOLIDADO_NO_ENCONTRADO");
        ex.Which.EstadoHttp.Should().Be(404);
    }

    private static ResultadoConsolidadoRanking CrearResultado(decimal puntaje)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);
    }
}
