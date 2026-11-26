using System.Reflection;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Consultas;

public sealed class ObtenerMisAsignacionesQueryHandlerTests
{
    private static readonly Guid _evaluadorAutenticadoId = Guid.Parse("01967000-0000-7000-8000-000000000701");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000702");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IAsignacionEvaluacionRepository _asignacionRepository =
        Substitute.For<IAsignacionEvaluacionRepository>();
    private readonly ObtenerMisAsignacionesQueryHandler _handler;

    public ObtenerMisAsignacionesQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_evaluadorAutenticadoId);

        _asignacionRepository
            .ListarPorEvaluadorAsync(
                Arg.Any<Guid>(),
                Arg.Any<EstadoAsignacionEvaluacion?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PaginaResultado<AsignacionEvaluacion>([], 0)));

        _handler = new ObtenerMisAsignacionesQueryHandler(
            _usuarioActual,
            _asignacionRepository,
            new ObtenerMisAsignacionesQueryValidator());
    }

    [Fact]
    public void Debe_NoAceptarUsuarioPorParametro_Cuando_SeInspeccionaObtenerMisAsignacionesQuery()
    {
        var propiedades = typeof(ObtenerMisAsignacionesQuery)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        propiedades.Should().BeEquivalentTo(
            [
                nameof(ObtenerMisAsignacionesQuery.Estado),
                nameof(ObtenerMisAsignacionesQuery.Pagina),
                nameof(ObtenerMisAsignacionesQuery.Tamano)
            ]);
    }

    [Fact]
    public async Task Debe_FiltrarSiemprePorUsuarioAutenticadoConPaginacionYEstado_Cuando_EsValida()
    {
        using var cts = new CancellationTokenSource();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Excelencia",
            Guid.NewGuid(),
            _fechaBase,
            _coordinadorId);
        var miembro = comite.AgregarMiembro(
            _evaluadorAutenticadoId,
            TipoEvaluador.Interno,
            _fechaBase.AddDays(1),
            _coordinadorId);
        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            Guid.NewGuid(),
            _fechaBase.AddDays(2),
            _fechaBase.AddDays(12),
            _coordinadorId);

        _asignacionRepository
            .ListarPorEvaluadorAsync(
                _evaluadorAutenticadoId,
                EstadoAsignacionEvaluacion.Pendiente,
                2,
                10,
                cts.Token)
            .Returns(Task.FromResult(new PaginaResultado<AsignacionEvaluacion>([asignacion], 15)));

        var resultado = await _handler.HandleAsync(
            new ObtenerMisAsignacionesQuery(EstadoAsignacionEvaluacion.Pendiente, 2, 10),
            cts.Token);

        resultado.Pagina.Should().Be(2);
        resultado.Tamano.Should().Be(10);
        resultado.Total.Should().Be(15);
        resultado.Items.Should().ContainSingle();
        resultado.Items[0].AsignacionEvaluacionId.Should().Be(asignacion.Id);
        resultado.Items[0].PostulacionId.Should().Be(asignacion.PostulacionId);
        resultado.Items[0].ConvocatoriaId.Should().Be(comite.ConvocatoriaId);
        resultado.Items[0].Estado.Should().Be("PENDIENTE");

        await _asignacionRepository.Received(1).ListarPorEvaluadorAsync(
            _evaluadorAutenticadoId,
            EstadoAsignacionEvaluacion.Pendiente,
            2,
            10,
            cts.Token);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Debe_RechazarConValidacion_Cuando_PaginacionEsInvalida(int pagina, int tamano)
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerMisAsignacionesQuery(null, pagina, tamano),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .ListarPorEvaluadorAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_EstadoEsIndefinido()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerMisAsignacionesQuery((EstadoAsignacionEvaluacion)999, 1, 20),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .ListarPorEvaluadorAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new ObtenerMisAsignacionesQuery(),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);
        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .ListarPorEvaluadorAsync(default, default, default, default, default);
    }
}
