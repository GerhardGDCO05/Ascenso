using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerCargaEvaluadoresComite;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Consultas;

public sealed class ObtenerCargaEvaluadoresComiteQueryHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000601");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IComiteEvaluadorRepository _comiteRepository =
        Substitute.For<IComiteEvaluadorRepository>();
    private readonly IAsignacionEvaluacionRepository _asignacionRepository =
        Substitute.For<IAsignacionEvaluacionRepository>();
    private readonly ObtenerCargaEvaluadoresComiteQueryHandler _handler;

    public ObtenerCargaEvaluadoresComiteQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _comiteRepository
            .ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(null));
        _asignacionRepository
            .ObtenerCargaPorMiembroAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>()));

        _handler = new ObtenerCargaEvaluadoresComiteQueryHandler(
            _usuarioActual,
            _comiteRepository,
            _asignacionRepository,
            new ObtenerCargaEvaluadoresComiteQueryValidator());
    }

    [Fact]
    public async Task Debe_DevolverSoloMiembrosActivosIncluyendoCargaCero_Cuando_ComiteExiste()
    {
        using var cts = new CancellationTokenSource();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Excelencia",
            Guid.NewGuid(),
            _fechaBase,
            _coordinadorId);
        var jefatura = comite.Miembros.Single();
        var activoSinCarga = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);
        var desvinculado = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaBase.AddDays(2),
            _coordinadorId);
        comite.DesvincularMiembro(desvinculado.Id, _fechaBase.AddDays(3), _coordinadorId);

        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        IReadOnlyDictionary<Guid, int> carga = new Dictionary<Guid, int>
        {
            [jefatura.Id] = 4,
            [desvinculado.Id] = 2
        };

        _asignacionRepository
            .ObtenerCargaPorMiembroAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult(carga));

        var resultado = await _handler.HandleAsync(
            new ObtenerCargaEvaluadoresComiteQuery(comite.Id),
            cts.Token);

        resultado.Should().HaveCount(2);
        resultado.Should().NotContain(item => item.MiembroComiteId == desvinculado.Id);

        var cargaCero = resultado.Single(item => item.MiembroComiteId == activoSinCarga.Id);
        cargaCero.AsignacionesAbiertas.Should().Be(0);
        cargaCero.Tipo.Should().Be("EXTERNO");
        cargaCero.Rol.Should().Be("EVALUADOR");

        var cargaJefatura = resultado.Single(item => item.MiembroComiteId == jefatura.Id);
        cargaJefatura.AsignacionesAbiertas.Should().Be(4);
        cargaJefatura.Tipo.Should().Be("INTERNO");
        cargaJefatura.Rol.Should().Be("JEFATURA");
    }

    [Fact]
    public async Task Debe_RechazarCon404ComiteNoEncontrado_Cuando_ElComiteNoExiste()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerCargaEvaluadoresComiteQuery(Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ComiteNoEncontradoException>()
            .Where(ex => ex.Codigo == "COMITE_NO_ENCONTRADO" && ex.EstadoHttp == 404);

        await _asignacionRepository.DidNotReceiveWithAnyArgs()
            .ObtenerCargaPorMiembroAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ComiteIdEstaVacio()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerCargaEvaluadoresComiteQuery(Guid.Empty),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();
        await _comiteRepository.DidNotReceiveWithAnyArgs().ObtenerPorIdAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new ObtenerCargaEvaluadoresComiteQuery(Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(ex => ex.Codigo == "NO_AUTENTICADO" && ex.EstadoHttp == 401);
    }
}
