using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ListarResultadosConsolidadosConvocatoria;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Consultas;

public sealed class ListarResultadosConsolidadosConvocatoriaQueryHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000002501");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000002502");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000002503");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly IResultadoConsolidadoRepository _resultadoRepository = Substitute.For<IResultadoConsolidadoRepository>();
    private readonly ListarResultadosConsolidadosConvocatoriaQueryHandler _handler;

    public ListarResultadosConsolidadosConvocatoriaQueryHandlerTests()
    {
        _handler = new ListarResultadosConsolidadosConvocatoriaQueryHandler(
            _resultadoRepository,
            new ListarResultadosConsolidadosConvocatoriaQueryValidator());
    }

    [Fact]
    public async Task Debe_ListarPaginadoConFiltrosYPropagarCancellationToken_Cuando_ConsultaEsValida()
    {
        using var cts = new CancellationTokenSource();
        var r1 = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(_convocatoriaId, _postulacion1Id, false, null, null, 2, 90.00m, 25.00m, 20.00m, true),
            _fechaBase,
            UsuarioSistema.Id);
        var r2 = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(_convocatoriaId, _postulacion2Id, false, null, null, 2, 82.00m, 22.00m, 20.00m, true),
            _fechaBase.AddMinutes(5),
            UsuarioSistema.Id);

        _resultadoRepository
            .ListarPorConvocatoriaAsync(_convocatoriaId, true, false, 1, 20, cts.Token)
            .Returns(Task.FromResult(new PaginaResultado<ResultadoConsolidadoRanking>([r1, r2], 2)));

        var query = new ListarResultadosConsolidadosConvocatoriaQuery(
            _convocatoriaId,
            AlertaDiscrepancia: true,
            DiscrepanciaRevisada: false,
            Pagina: 1,
            Tamano: 20);

        var pagina = await _handler.HandleAsync(query, cts.Token);

        pagina.Pagina.Should().Be(1);
        pagina.Tamano.Should().Be(20);
        pagina.Total.Should().Be(2);
        pagina.Items.Should().HaveCount(2);
        pagina.Items[0].PostulacionId.Should().Be(_postulacion1Id);
        pagina.Items[0].EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        pagina.Items[1].PostulacionId.Should().Be(_postulacion2Id);

        await _resultadoRepository.Received(1)
            .ListarPorConvocatoriaAsync(_convocatoriaId, true, false, 1, 20, cts.Token);
    }

    [Fact]
    public async Task Debe_FallarValidacion_Cuando_ConvocatoriaIdEsVacio()
    {
        var query = new ListarResultadosConsolidadosConvocatoriaQuery(Guid.Empty);

        var accion = () => _handler.HandleAsync(query);

        await accion.Should().ThrowAsync<ValidationException>();
        await _resultadoRepository.DidNotReceiveWithAnyArgs()
            .ListarPorConvocatoriaAsync(default, default, default, default, default, default);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(1, 101)]
    public async Task Debe_FallarValidacion_Cuando_PaginacionEstaFueraDeRango(int pagina, int tamano)
    {
        var query = new ListarResultadosConsolidadosConvocatoriaQuery(
            _convocatoriaId,
            Pagina: pagina,
            Tamano: tamano);

        var accion = () => _handler.HandleAsync(query);

        await accion.Should().ThrowAsync<ValidationException>();
        await _resultadoRepository.DidNotReceiveWithAnyArgs()
            .ListarPorConvocatoriaAsync(default, default, default, default, default, default);
    }
}
