using System.Reflection;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMisEvaluaciones;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Consultas;

public sealed class ObtenerMisEvaluacionesQueryHandlerTests
{
    private static readonly Guid _evaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000601");
    private static readonly Guid _coordinadorId = Guid.Parse("01968100-0000-7000-8000-000000000602");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IEvaluacionRepository _evaluacionRepository =
        Substitute.For<IEvaluacionRepository>();
    private readonly ObtenerMisEvaluacionesQueryHandler _handler;

    public ObtenerMisEvaluacionesQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorId);
        _evaluacionRepository
            .ListarPorEvaluadorAsync(
                Arg.Any<Guid>(),
                Arg.Any<EstadoEvaluacion?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PaginaResultado<Evaluacion>([], 0)));

        _handler = new ObtenerMisEvaluacionesQueryHandler(
            _usuarioActual,
            _evaluacionRepository,
            new ObtenerMisEvaluacionesQueryValidator());
    }

    [Fact]
    public void Debe_NoAceptarEvaluadorPorParametro_Cuando_SeInspeccionaObtenerMisEvaluacionesQuery()
    {
        var propiedades = typeof(ObtenerMisEvaluacionesQuery)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        propiedades.Should().BeEquivalentTo(
            [
                nameof(ObtenerMisEvaluacionesQuery.Estado),
                nameof(ObtenerMisEvaluacionesQuery.Pagina),
                nameof(ObtenerMisEvaluacionesQuery.Tamano)
            ]);
    }

    [Fact]
    public async Task Debe_ListarEvaluacionesPropiasConPaginacionYEstado_Cuando_ConsultaEsValida()
    {
        using var cts = new CancellationTokenSource();
        var evaluacion = CrearEvaluacionBorrador();

        _evaluacionRepository
            .ListarPorEvaluadorAsync(_evaluadorId, EstadoEvaluacion.Borrador, 2, 15, cts.Token)
            .Returns(Task.FromResult(new PaginaResultado<Evaluacion>([evaluacion], 22)));

        var resultado = await _handler.HandleAsync(
            new ObtenerMisEvaluacionesQuery(EstadoEvaluacion.Borrador, 2, 15),
            cts.Token);

        resultado.Pagina.Should().Be(2);
        resultado.Tamano.Should().Be(15);
        resultado.Total.Should().Be(22);
        resultado.Items.Should().ContainSingle();
        resultado.Items[0].EvaluacionId.Should().Be(evaluacion.Id);
        resultado.Items[0].Estado.Should().Be("BORRADOR");
        resultado.Items[0].CantidadCriteriosCalificados.Should().Be(1);

        await _evaluacionRepository.Received(1)
            .ListarPorEvaluadorAsync(_evaluadorId, EstadoEvaluacion.Borrador, 2, 15, cts.Token);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Debe_RechazarConValidacion_Cuando_PaginacionEstaFueraDeRango(int pagina, int tamano)
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerMisEvaluacionesQuery(null, pagina, tamano),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
        await _evaluacionRepository.DidNotReceiveWithAnyArgs()
            .ListarPorEvaluadorAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_EstadoNoEstaDefinido()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerMisEvaluacionesQuery((EstadoEvaluacion)999, 1, 20),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(new ObtenerMisEvaluacionesQuery(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);
    }

    private static Evaluacion CrearEvaluacionBorrador()
    {
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(programaId, convocatoriaId, "Comité", Guid.NewGuid(), _fechaBase, _coordinadorId);
        var miembro = comite.AgregarMiembro(_evaluadorId, TipoEvaluador.Interno, _fechaBase.AddDays(1), _coordinadorId);
        var asignacion = AsignacionEvaluacion.Crear(comite, miembro.Id, Guid.NewGuid(), _fechaBase.AddDays(2), _fechaBase.AddDays(12), _coordinadorId);
        var rubrica = RubricaEvaluacion.Crear(programaId, convocatoriaId, "Rúbrica", _fechaBase, _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc", 100m, 0m, 20m, _fechaBase.AddMinutes(1), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);

        return Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 15m, null)],
            null,
            _fechaBase.AddDays(3),
            _evaluadorId);
    }
}
