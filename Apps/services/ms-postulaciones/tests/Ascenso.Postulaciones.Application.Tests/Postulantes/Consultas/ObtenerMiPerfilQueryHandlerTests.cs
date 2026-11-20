using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Postulantes.Consultas;

public sealed class ObtenerMiPerfilQueryHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000011");
    private static readonly Guid _universidadId = Guid.Parse("01966000-0000-7000-8000-000000000012");
    private static readonly Guid _carreraId = Guid.Parse("01966000-0000-7000-8000-000000000013");
    private static readonly Guid _periodoId = Guid.Parse("01966000-0000-7000-8000-000000000014");
    private static readonly DateTimeOffset _fechaRegistro =
        new(2026, 11, 14, 13, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IPostulanteRepository _postulanteRepository = Substitute.For<IPostulanteRepository>();
    private readonly ObtenerMiPerfilQueryHandler _handler;

    public ObtenerMiPerfilQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_usuarioId);

        _handler = new ObtenerMiPerfilQueryHandler(_usuarioActual, _postulanteRepository);
    }

    [Fact]
    public async Task Debe_DevolverElPerfilDelUsuarioAutenticado_Cuando_Existe()
    {
        _postulanteRepository
            .ObtenerPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(CrearPostulante()));

        var resultado = await _handler.HandleAsync(new ObtenerMiPerfilQuery(), CancellationToken.None);

        resultado.UsuarioId.Should().Be(_usuarioId);
        resultado.NumeroDocumentoIdentidad.Should().Be("12345678");
        resultado.NombreCompleto.Should().Be("Ana Pérez Gómez");
        resultado.DatosContacto.CorreoElectronico.Should().Be("ana.perez@example.com");
        resultado.DatosContacto.Telefono.Should().Be("+51999888777");
        resultado.CondicionAcademica.Should().Be("ADMITIDO");
        resultado.EstadoPerfil.Should().Be("INCOMPLETO");
        resultado.FechaRegistro.Should().Be(_fechaRegistro);

        await _postulanteRepository.Received(1)
            .ObtenerPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_DevolverLosDatosSocioeconomicosYFamiliares_Cuando_ElPerfilEstaCompleto()
    {
        var postulante = CrearPostulante();
        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Casado,
            17.25m,
            true,
            Ocupacion.Empleado,
            1500.50m,
            [
                new DatosMiembroFamiliar(Parentesco.Pareja, 40, Ocupacion.Independiente, 800.25m),
                new DatosMiembroFamiliar(Parentesco.Hijo, 10, Ocupacion.Estudiante, 0m)
            ],
            _usuarioId,
            _fechaRegistro.AddDays(1));

        _postulanteRepository
            .ObtenerPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(postulante));

        var resultado = await _handler.HandleAsync(new ObtenerMiPerfilQuery(), CancellationToken.None);

        resultado.EstadoCivil.Should().Be("CASADO");
        resultado.PromedioActual.Should().Be(17.25m);
        resultado.Trabaja.Should().BeTrue();
        resultado.Ocupacion.Should().Be("EMPLEADO");
        resultado.IngresoMensualPostulante.Should().Be(1500.50m);
        resultado.NumeroIntegrantesFamilia.Should().Be(3);
        resultado.IngresoFamiliarMensualTotal.Should().Be(2300.75m);
        resultado.MiembrosFamiliares.Should().HaveCount(2);
        resultado.MiembrosFamiliares[0].Parentesco.Should().Be("PAREJA");
        resultado.MiembrosFamiliares[0].Edad.Should().Be(40);
        resultado.MiembrosFamiliares[0].Ocupacion.Should().Be("INDEPENDIENTE");
        resultado.MiembrosFamiliares[0].IngresoMensual.Should().Be(800.25m);
        resultado.MiembrosFamiliares[1].Parentesco.Should().Be("HIJO");
        resultado.EstadoPerfil.Should().Be("COMPLETO");
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_ElPerfilNoExiste()
    {
        _postulanteRepository
            .ObtenerPorUsuarioIdAsync(_usuarioId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Postulante?>(null));

        var accion = () => _handler.HandleAsync(new ObtenerMiPerfilQuery(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<PostulanteNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "POSTULANTE_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(new ObtenerMiPerfilQuery(), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(excepcion => excepcion.Codigo == "NO_AUTENTICADO" && excepcion.EstadoHttp == 401);

        await _postulanteRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorUsuarioIdAsync(default, default);
    }

    private static Postulante CrearPostulante()
        => Postulante.Crear(
            personaId: null,
            _usuarioId,
            "12345678",
            "Ana",
            "Pérez Gómez",
            new DatosContacto("ana.perez@example.com", "+51 999 888 777"),
            _universidadId,
            _carreraId,
            _periodoId,
            CondicionAcademicaIngreso.Admitido,
            aceptaTratamientoDatos: true,
            _fechaRegistro);
}
