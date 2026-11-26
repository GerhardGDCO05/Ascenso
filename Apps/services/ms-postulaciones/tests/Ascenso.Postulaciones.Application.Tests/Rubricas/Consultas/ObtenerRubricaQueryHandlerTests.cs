using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Consultas.ObtenerRubrica;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.Consultas;

public sealed class ObtenerRubricaQueryHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000711");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000712");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000713");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IRubricaRepository _rubricaRepository = Substitute.For<IRubricaRepository>();
    private readonly ObtenerRubricaQueryHandler _handler;

    public ObtenerRubricaQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _rubricaRepository
            .ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RubricaEvaluacion?>(null));

        _handler = new ObtenerRubricaQueryHandler(_usuarioActual, _rubricaRepository);
    }

    [Fact]
    public async Task Debe_DevolverLaRubricaConCriteriosSumaYDiferencia_Cuando_Existe()
    {
        using var cts = new CancellationTokenSource();
        var rubrica = CrearRubricaIncompleta();
        _rubricaRepository
            .ObtenerPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));

        var resultado = await _handler.HandleAsync(
            new ObtenerRubricaQuery(rubrica.Id),
            cts.Token);

        resultado.Id.Should().Be(rubrica.Id);
        resultado.ProgramaBecaId.Should().Be(_programaBecaId);
        resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        resultado.Nombre.Should().Be("Rúbrica Beca Excelencia");
        resultado.Estado.Should().Be("BORRADOR");
        resultado.FechaPublicacion.Should().BeNull();
        resultado.Criterios.Should().HaveCount(2);
        resultado.Criterios[0].Orden.Should().Be(1);
        resultado.Criterios[0].PesoPorcentual.Should().Be(60.00m);
        resultado.Criterios[1].Orden.Should().Be(2);
        resultado.Criterios[1].PesoPorcentual.Should().Be(30.50m);
        resultado.SumaPesos.Should().Be(90.50m);
        resultado.DiferenciaPara100.Should().Be(9.50m);

        await _rubricaRepository.Received(1)
            .ObtenerPorIdAsync(rubrica.Id, cts.Token);
        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ObtenerParaActualizarPorIdAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new ObtenerRubricaQuery(Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(excepcion => excepcion.Codigo == "NO_AUTENTICADO" && excepcion.EstadoHttp == 401);

        await _rubricaRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorIdAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_LaRubricaNoExiste()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerRubricaQuery(Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<RubricaNoEncontradaException>()
            .Where(excepcion => excepcion.Codigo == "RUBRICA_NO_ENCONTRADA" && excepcion.EstadoHttp == 404);
    }

    private static RubricaEvaluacion CrearRubricaIncompleta()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia",
            _fechaCreacion,
            _usuarioId);

        rubrica.AgregarCriterio(
            "Rendimiento académico",
            "Promedio ponderado",
            60.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        rubrica.AgregarCriterio(
            "Situación socioeconómica",
            "Ingreso familiar",
            30.50m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(10),
            _usuarioId);

        return rubrica;
    }
}
