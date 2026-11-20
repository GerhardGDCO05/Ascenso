using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Postulantes.Comandos;

public sealed class ActualizarPerfilSocioeconomicoCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000021");
    private static readonly Guid _universidadId = Guid.Parse("01966000-0000-7000-8000-000000000022");
    private static readonly Guid _carreraId = Guid.Parse("01966000-0000-7000-8000-000000000023");
    private static readonly Guid _periodoId = Guid.Parse("01966000-0000-7000-8000-000000000024");
    private static readonly DateTimeOffset _fechaRegistro =
        new(2026, 11, 14, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaActualizacion =
        new(2026, 11, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IPostulanteRepository _postulanteRepository = Substitute.For<IPostulanteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ActualizarPerfilSocioeconomicoCommandHandler _handler;

    public ActualizarPerfilSocioeconomicoCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _postulanteRepository
            .ObtenerParaActualizarPorUsuarioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(null));
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new ActualizarPerfilSocioeconomicoCommandHandler(
            _usuarioActual,
            _postulanteRepository,
            _unitOfWork,
            new ActualizarPerfilSocioeconomicoCommandValidator(),
            new RelojFijo(_fechaActualizacion));
    }

    [Fact]
    public async Task Debe_ActualizarElPerfil_Cuando_LaListaFamiliarEstaVacia()
    {
        _postulanteRepository
            .ObtenerParaActualizarPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(CrearPostulanteExistente()));

        var resultado = await _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        resultado.EstadoCivil.Should().Be("CASADO");
        resultado.PromedioActual.Should().Be(17.5m);
        resultado.Trabaja.Should().BeTrue();
        resultado.Ocupacion.Should().Be("EMPLEADO");
        resultado.IngresoMensualPostulante.Should().Be(1500m);
        resultado.NumeroIntegrantesFamilia.Should().Be(1);
        resultado.IngresoFamiliarMensualTotal.Should().Be(1500m);
        resultado.MiembrosFamiliares.Should().BeEmpty();
        resultado.EstadoPerfil.Should().Be("COMPLETO");

        await _postulanteRepository.Received(1)
            .ObtenerParaActualizarPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ActualizarElPerfilYLosFamiliares_Cuando_LaListaTieneMiembros()
    {
        _postulanteRepository
            .ObtenerParaActualizarPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(CrearPostulanteExistente()));

        var comando = CrearComandoValido(
        [
            new DatosMiembroFamiliarCommand("PAREJA", 40, "INDEPENDIENTE", 800.25m),
            new DatosMiembroFamiliarCommand("HIJO", 10, "ESTUDIANTE", 0m)
        ]);

        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        resultado.NumeroIntegrantesFamilia.Should().Be(3);
        resultado.IngresoFamiliarMensualTotal.Should().Be(2300.25m);
        resultado.MiembrosFamiliares.Should().HaveCount(2);
        resultado.MiembrosFamiliares[0].Id.Should().NotBeEmpty();
        resultado.MiembrosFamiliares[0].Parentesco.Should().Be("PAREJA");
        resultado.MiembrosFamiliares[0].Edad.Should().Be(40);
        resultado.MiembrosFamiliares[0].Ocupacion.Should().Be("INDEPENDIENTE");
        resultado.MiembrosFamiliares[0].IngresoMensual.Should().Be(800.25m);
        resultado.MiembrosFamiliares[1].Parentesco.Should().Be("HIJO");
        resultado.MiembrosFamiliares[1].Edad.Should().Be(10);
        resultado.MiembrosFamiliares[1].Ocupacion.Should().Be("ESTUDIANTE");
        resultado.EstadoPerfil.Should().Be("COMPLETO");

        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_ElPerfilNoExiste()
    {
        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PostulanteNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "POSTULANTE_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElCatalogoEsInvalido()
    {
        var comando = CrearComandoValido() with { EstadoCivil = "CONVIVIENTE" };

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _postulanteRepository.DidNotReceiveWithAnyArgs()
            .ObtenerParaActualizarPorUsuarioIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarConValidacion_Cuando_ElCatalogoDeUnFamiliarEsInvalido()
    {
        var comando = CrearComandoValido(
        [
            new DatosMiembroFamiliarCommand("TIO", 30, "OTRO", 0m)
        ]);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(excepcion => excepcion.Codigo == "NO_AUTENTICADO" && excepcion.EstadoHttp == 401);

        await _postulanteRepository.DidNotReceiveWithAnyArgs()
            .ObtenerParaActualizarPorUsuarioIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static ActualizarPerfilSocioeconomicoCommand CrearComandoValido(
        IReadOnlyList<DatosMiembroFamiliarCommand>? miembros = null)
        => new(
            "CASADO",
            17.5m,
            true,
            "EMPLEADO",
            1500m,
            miembros ?? []);

    private static Postulante CrearPostulanteExistente()
        => Postulante.Crear(
            personaId: null,
            _usuarioId,
            "12345678",
            "Ana",
            "Pérez Gómez",
            new DatosContacto("ana.perez@example.com", "+51999888777"),
            _universidadId,
            _carreraId,
            _periodoId,
            CondicionAcademicaIngreso.Admitido,
            aceptaTratamientoDatos: true,
            _fechaRegistro);

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
