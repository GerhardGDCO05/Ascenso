using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.ListaEspera.Consultas;

public sealed class ConsultarAdjudicabilidadPromovidaTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01969000-0000-7000-8000-000000003001");
    private static readonly Guid _programaBecaId = Guid.Parse("01969000-0000-7000-8000-000000003002");
    private static readonly Guid _postulacionId = Guid.Parse("01969000-0000-7000-8000-000000003003");
    private static readonly Guid _coordinadorId = Guid.Parse("01969000-0000-7000-8000-000000003004");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly IResultadoConsolidadoRepository _resultadoRepository = Substitute.For<IResultadoConsolidadoRepository>();
    private readonly IEntradaListaEsperaRepository _entradaRepository = Substitute.For<IEntradaListaEsperaRepository>();
    private readonly ConsultarAdjudicabilidadPostulacionQueryValidator _validator = new();

    private ConsultarAdjudicabilidadPostulacionQueryHandler CrearHandler()
        => new(_resultadoRepository, _entradaRepository, _validator);

    [Fact]
    public async Task Debe_RetornarAdjudicableTrue_Cuando_PostulacionEnListaDeEsperaFuePromovida()
    {
        using var cts = new CancellationTokenSource();
        var resultado = CrearResultadoConfirmado(ClasificacionRanking.ListaDeEspera, posicion: 3, monto: 1200.00m);
        var entrada = EntradaListaEspera.Crear(
            _convocatoriaId, _programaBecaId, _postulacionId, posicionOrden: 1, 1200.00m, _fechaBase, _coordinadorId);
        var fechaPromocion = _fechaBase.AddHours(3);
        entrada.Promover(fechaPromocion, _coordinadorId, "Vacante liberada confirmada");

        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, cts.Token).Returns(resultado);
        _entradaRepository.ObtenerPorPostulacionAsync(_postulacionId, cts.Token).Returns(entrada);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId), cts.Token);

        dto.Adjudicable.Should().BeTrue();
        dto.MotivoNoAdjudicable.Should().BeNull();
        dto.PromovidaDeListaEspera.Should().BeTrue();
        dto.EstadoListaEspera.Should().Be("PROMOVIDA");
        dto.PosicionListaEspera.Should().Be(1);
        dto.FechaPromocionListaEspera.Should().Be(fechaPromocion);
        dto.ProgramaBecaId.Should().Be(_programaBecaId);
        dto.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dto.Clasificacion.Should().Be("LISTA_DE_ESPERA");
        dto.PosicionRanking.Should().Be(3);
        dto.MontoEstimadoBeca.Should().Be(1200.00m);
        await _entradaRepository.Received(1).ObtenerPorPostulacionAsync(_postulacionId, cts.Token);
    }

    [Fact]
    public async Task Debe_RetornarAdjudicableFalse_Cuando_PostulacionEnListaDeEsperaEstaSoloElegiblePorVacante()
    {
        var resultado = CrearResultadoConfirmado(ClasificacionRanking.ListaDeEspera, posicion: 3, monto: 1200.00m);
        var entrada = EntradaListaEspera.Crear(
            _convocatoriaId, _programaBecaId, _postulacionId, posicionOrden: 1, 1200.00m, _fechaBase, _coordinadorId);
        entrada.MarcarElegiblePorVacante(_fechaBase.AddHours(1), UsuarioSistema.Id);

        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>()).Returns(resultado);
        _entradaRepository.ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>()).Returns(entrada);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId));

        dto.Adjudicable.Should().BeFalse();
        dto.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoFueraDeCupoEnListaDeEspera);
        dto.PromovidaDeListaEspera.Should().BeFalse();
        dto.EstadoListaEspera.Should().Be("ELEGIBLE_POR_VACANTE");
        dto.PosicionListaEspera.Should().Be(1);
        dto.FechaPromocionListaEspera.Should().BeNull();
    }

    [Fact]
    public async Task Debe_NoConsultarRepositorioListaEspera_Cuando_ResultadoEsDentroDeCupoOProvisional()
    {
        var dentro = CrearResultadoConfirmado(ClasificacionRanking.DentroDeCupo, posicion: 1, monto: 1500.00m);
        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>()).Returns(dentro);

        var handler = CrearHandler();
        var dtoDentro = await handler.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId));

        dtoDentro.Adjudicable.Should().BeTrue();
        dtoDentro.PromovidaDeListaEspera.Should().BeFalse();
        await _entradaRepository.DidNotReceiveWithAnyArgs().ObtenerPorPostulacionAsync(default, default);

        var provisional = CrearResultadoProvisional();
        _resultadoRepository.ObtenerPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>()).Returns(provisional);

        var dtoProvisional = await handler.HandleAsync(new ConsultarAdjudicabilidadPostulacionQuery(_postulacionId));

        dtoProvisional.Adjudicable.Should().BeFalse();
        dtoProvisional.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoRankingNoConfirmado);
        await _entradaRepository.DidNotReceiveWithAnyArgs().ObtenerPorPostulacionAsync(default, default);
    }

    private static ResultadoConsolidadoRanking CrearResultadoProvisional()
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 88.00m,
            diferenciaEntreEvaluadores: 2.00m,
            umbralDiscrepanciaAplicado: 15.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);
    }

    private static ResultadoConsolidadoRanking CrearResultadoConfirmado(
        ClasificacionRanking clasificacion,
        int posicion,
        decimal monto)
    {
        var resultado = CrearResultadoProvisional();
        resultado.ConfirmarRanking(posicion, monto, clasificacion, _fechaBase, _coordinadorId);
        return resultado;
    }
}
