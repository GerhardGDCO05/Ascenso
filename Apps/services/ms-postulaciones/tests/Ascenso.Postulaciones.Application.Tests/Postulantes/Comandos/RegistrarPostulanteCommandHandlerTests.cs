using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Postulantes.Comandos;

public sealed class RegistrarPostulanteCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000001");
    private static readonly Guid _universidadId = Guid.Parse("01966000-0000-7000-8000-000000000002");
    private static readonly Guid _carreraId = Guid.Parse("01966000-0000-7000-8000-000000000003");
    private static readonly Guid _periodoId = Guid.Parse("01966000-0000-7000-8000-000000000004");
    private static readonly DateTimeOffset _fechaRegistro =
        new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IPostulanteRepository _postulanteRepository = Substitute.For<IPostulanteRepository>();
    private readonly IPeriodoAcademicoClient _periodoAcademicoClient = Substitute.For<IPeriodoAcademicoClient>();
    private readonly ICatalogoAcademicoClient _catalogoAcademicoClient = Substitute.For<ICatalogoAcademicoClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegistrarPostulanteCommandHandler _handler;

    public RegistrarPostulanteCommandHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _postulanteRepository
            .ObtenerPorUsuarioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(null));
        _postulanteRepository
            .ExistePorNumeroDocumentoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _postulanteRepository
            .AgregarAsync(Arg.Any<Postulante>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork
            .GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _periodoAcademicoClient
            .ObtenerPeriodoActualAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CrearPeriodoVigente()));
        _catalogoAcademicoClient
            .ObtenerUniversidadAsync(_universidadId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CrearUniversidadElegible()));

        _handler = new RegistrarPostulanteCommandHandler(
            _usuarioActual,
            _postulanteRepository,
            _periodoAcademicoClient,
            _catalogoAcademicoClient,
            _unitOfWork,
            new RegistrarPostulanteCommandValidator(),
            new RelojFijo(_fechaRegistro));
    }

    [Fact]
    public async Task Debe_RegistrarElPerfilEnEstadoIncompleto_Cuando_LosDatosSonValidos()
    {
        var comando = CrearComandoValido();

        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        resultado.Id.Should().NotBeEmpty();
        resultado.UsuarioId.Should().Be(_usuarioId);
        resultado.PersonaId.Should().BeNull();
        resultado.NumeroDocumentoIdentidad.Should().Be("12345678");
        resultado.NombreCompleto.Should().Be("Ana Pérez Gómez");
        resultado.DatosContacto.CorreoElectronico.Should().Be("ana.perez@example.com");
        resultado.DatosContacto.Telefono.Should().Be("+51999888777");
        resultado.CondicionAcademica.Should().Be("ADMITIDO");
        resultado.EstadoPerfil.Should().Be("INCOMPLETO");
        resultado.AceptaTratamientoDatos.Should().BeTrue();
        resultado.FechaRegistro.Should().Be(_fechaRegistro);

        await _postulanteRepository.Received(1).AgregarAsync(
            Arg.Is<Postulante>(postulante =>
                postulante.UsuarioId == _usuarioId
                && postulante.NumeroDocumentoIdentidad == "12345678"
                && postulante.EstadoPerfil == EstadoPerfil.Incompleto),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
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
            .ObtenerPorUsuarioIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409_Cuando_ElUsuarioYaTienePerfil()
    {
        _postulanteRepository
            .ObtenerPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(CrearPostulanteExistente()));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PerfilYaRegistradoException>()
            .Where(excepcion => excepcion.Codigo == "PERFIL_YA_REGISTRADO" && excepcion.EstadoHttp == 409);

        await _postulanteRepository.DidNotReceiveWithAnyArgs()
            .ExistePorNumeroDocumentoAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon409_Cuando_ElDocumentoYaEstaRegistrado()
    {
        _postulanteRepository
            .ExistePorNumeroDocumentoAsync("12345678", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<IdentidadDuplicadaException>()
            .Where(excepcion => excepcion.Codigo == "IDENTIDAD_DUPLICADA" && excepcion.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422_Cuando_ElPeriodoNoEsElVigente()
    {
        _periodoAcademicoClient
            .ObtenerPeriodoActualAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                new PeriodoAcademicoActual(Guid.NewGuid(), "2026-II", EstadoPeriodoAcademico.Vigente)));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PeriodoAcademicoInvalidoException>()
            .Where(excepcion => excepcion.Codigo == "PERIODO_ACADEMICO_INVALIDO" && excepcion.EstadoHttp == 422);

        await _catalogoAcademicoClient.DidNotReceiveWithAnyArgs()
            .ObtenerUniversidadAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon503_Cuando_ElCalendarioNoEstaDisponible()
    {
        _periodoAcademicoClient
            .ObtenerPeriodoActualAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PeriodoAcademicoActual>(new DependenciaNoDisponibleException()));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<DependenciaNoDisponibleException>()
            .Where(excepcion => excepcion.Codigo == "DEPENDENCIA_NO_DISPONIBLE" && excepcion.EstadoHttp == 503);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422_Cuando_LaUniversidadEstaInactiva()
    {
        _catalogoAcademicoClient
            .ObtenerUniversidadAsync(_universidadId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CrearUniversidadElegible(estado: EstadoUniversidadAliada.Inactiva)));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<UniversidadNoElegibleException>()
            .Where(excepcion => excepcion.Codigo == "UNIVERSIDAD_NO_ELEGIBLE" && excepcion.EstadoHttp == 422);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422_Cuando_LaUniversidadNoTieneConvenioVigente()
    {
        _catalogoAcademicoClient
            .ObtenerUniversidadAsync(_universidadId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CrearUniversidadElegible(tieneConvenioVigente: false)));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<UniversidadNoElegibleException>()
            .Where(excepcion => excepcion.Codigo == "UNIVERSIDAD_NO_ELEGIBLE" && excepcion.EstadoHttp == 422);
    }

    [Fact]
    public async Task Debe_RechazarCon422_Cuando_LaCarreraNoPerteneceALaUniversidad()
    {
        _catalogoAcademicoClient
            .ObtenerUniversidadAsync(_universidadId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CrearUniversidadElegible(carreras: [])));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<CarreraNoElegibleException>()
            .Where(excepcion => excepcion.Codigo == "CARRERA_NO_ELEGIBLE" && excepcion.EstadoHttp == 422);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422_Cuando_LaCarreraEstaInactiva()
    {
        _catalogoAcademicoClient
            .ObtenerUniversidadAsync(_universidadId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CrearUniversidadElegible(carreraActiva: false)));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<CarreraNoElegibleException>()
            .Where(excepcion => excepcion.Codigo == "CARRERA_NO_ELEGIBLE" && excepcion.EstadoHttp == 422);
    }

    [Fact]
    public async Task Debe_RechazarCon503_Cuando_ElCatalogoAcademicoNoEstaDisponible()
    {
        _catalogoAcademicoClient
            .ObtenerUniversidadAsync(_universidadId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UniversidadAliadaCatalogo>(new DependenciaNoDisponibleException()));

        var accion = () => _handler.HandleAsync(CrearComandoValido(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<DependenciaNoDisponibleException>()
            .Where(excepcion => excepcion.Codigo == "DEPENDENCIA_NO_DISPONIBLE" && excepcion.EstadoHttp == 503);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_RechazarCon422_Cuando_NoSeAceptaElTratamientoDeDatos()
    {
        var comando = CrearComandoValido() with { AceptaTratamientoDatos = false };

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ExcepcionReglaNegocio>()
            .Where(excepcion => excepcion.Codigo == "TRATAMIENTO_DATOS_NO_ACEPTADO" && excepcion.EstadoHttp == 422);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Theory]
    [InlineData("A", "ana@example.com", "999888777", "ADMITIDO")]
    [InlineData("Ana", "correo-invalido", "999888777", "ADMITIDO")]
    [InlineData("Ana", "ana@example.com", "", "ADMITIDO")]
    [InlineData("Ana", "ana@example.com", "999888777", "GRADUADO")]
    public async Task Debe_RechazarConValidacion_Cuando_ElComandoEsInvalido(
        string nombres,
        string correo,
        string telefono,
        string condicion)
    {
        var comando = CrearComandoValido() with
        {
            Nombres = nombres,
            DatosContacto = new DatosContactoDto(correo, telefono),
            CondicionAcademica = condicion
        };

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ValidationException>();

        await _postulanteRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorUsuarioIdAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static RegistrarPostulanteCommand CrearComandoValido()
        => new(
            " 12345678 ",
            " Ana ",
            " Pérez Gómez ",
            new DatosContactoDto("ana.perez@example.com", "+51 999 888 777"),
            _universidadId,
            _carreraId,
            _periodoId,
            "ADMITIDO",
            AceptaTratamientoDatos: true);

    private static PeriodoAcademicoActual CrearPeriodoVigente()
        => new(_periodoId, "2026-II", EstadoPeriodoAcademico.Vigente);

    private static UniversidadAliadaCatalogo CrearUniversidadElegible(
        EstadoUniversidadAliada estado = EstadoUniversidadAliada.Activa,
        bool tieneConvenioVigente = true,
        bool carreraActiva = true,
        IReadOnlyList<CarreraCatalogo>? carreras = null)
        => new(
            _universidadId,
            "Universidad Aliada",
            estado,
            tieneConvenioVigente,
            carreras ?? [new CarreraCatalogo(_carreraId, "Ingeniería de Sistemas", carreraActiva)]);

    private static Postulante CrearPostulanteExistente()
        => Postulante.Crear(
            personaId: null,
            _usuarioId,
            "87654321",
            "Otro",
            "Postulante",
            new DatosContacto("otro@example.com", "999111222"),
            _universidadId,
            _carreraId,
            _periodoId,
            CondicionAcademicaIngreso.Matriculado,
            aceptaTratamientoDatos: true,
            _fechaRegistro);

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
