using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerAsignacionesPostulacion;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Consultas;

public sealed class ObtenerAsignacionesPostulacionQueryHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000501");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000502");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IAsignacionEvaluacionRepository _asignacionRepository =
        Substitute.For<IAsignacionEvaluacionRepository>();
    private readonly ObtenerAsignacionesPostulacionQueryHandler _handler;

    public ObtenerAsignacionesPostulacionQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _asignacionRepository
            .ListarPorPostulacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([]));

        _handler = new ObtenerAsignacionesPostulacionQueryHandler(
            _usuarioActual,
            _asignacionRepository,
            new ObtenerAsignacionesPostulacionQueryValidator());
    }

    [Fact]
    public async Task Debe_DevolverListaVacia_Cuando_LaPostulacionNoTieneAsignaciones()
    {
        using var cts = new CancellationTokenSource();

        var resultado = await _handler.HandleAsync(
            new ObtenerAsignacionesPostulacionQuery(_postulacionId),
            cts.Token);

        resultado.PostulacionId.Should().Be(_postulacionId);
        resultado.Asignaciones.Should().BeEmpty();
        await _asignacionRepository.Received(1)
            .ListarPorPostulacionAsync(_postulacionId, cts.Token);
    }

    [Fact]
    public async Task Debe_DevolverAsignacionesOrdenadasPorFechaAsignacionEId_Cuando_ExistenAsignaciones()
    {
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Excelencia",
            Guid.NewGuid(),
            _fechaBase,
            _coordinadorId);
        var miembro1 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaBase.AddDays(1),
            _coordinadorId);
        var miembro2 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);

        var segunda = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            _postulacionId,
            _fechaBase.AddDays(3),
            _fechaBase.AddDays(15),
            _coordinadorId);
        var primera = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _fechaBase.AddDays(15),
            _coordinadorId);

        _asignacionRepository
            .ListarPorPostulacionAsync(_postulacionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AsignacionEvaluacion>>([segunda, primera]));

        var resultado = await _handler.HandleAsync(
            new ObtenerAsignacionesPostulacionQuery(_postulacionId),
            CancellationToken.None);

        resultado.Asignaciones.Should().HaveCount(2);
        resultado.Asignaciones[0].Id.Should().Be(primera.Id);
        resultado.Asignaciones[1].Id.Should().Be(segunda.Id);
        resultado.Asignaciones[0].Estado.Should().Be("PENDIENTE");
        resultado.Asignaciones[0].Version.Should().Be(1);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_PostulacionIdEstaVacio()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerAsignacionesPostulacionQuery(Guid.Empty),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .ListarPorPostulacionAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new ObtenerAsignacionesPostulacionQuery(_postulacionId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);
    }
}
