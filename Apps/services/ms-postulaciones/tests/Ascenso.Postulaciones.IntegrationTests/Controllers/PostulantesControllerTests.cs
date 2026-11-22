using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ascenso.Postulaciones.IntegrationTests.Controllers;

public sealed class PostulantesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderAutenticadoSinSub = "X-Test-Sin-Sub";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public PostulantesControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_Retornar201CreatedYLocation_Cuando_RegistroEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var universidadId = Guid.NewGuid();
        var carreraId = Guid.NewGuid();
        var periodoId = Guid.NewGuid();

        var repo = Substitute.For<IPostulanteRepository>();
        var periodoClient = Substitute.For<IPeriodoAcademicoClient>();
        var catalogoClient = Substitute.For<ICatalogoAcademicoClient>();
        var uow = Substitute.For<IUnitOfWork>();

        ConfigurarMocksValidos(repo, periodoClient, catalogoClient, periodoId, universidadId, carreraId);

        using var fabrica = CrearFabrica(repo, periodoClient, catalogoClient, uow);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/postulantes",
            CrearPayloadValido(universidadId, carreraId, periodoId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();
        respuesta.Headers.Location!.ToString().Should().Contain("/api/v1/postulantes/mio");

        var dto = await respuesta.Content.ReadFromJsonAsync<PostulanteDto>();
        dto.Should().NotBeNull();
        dto!.UsuarioId.Should().Be(usuarioId);
        dto.EstadoPerfil.Should().Be("INCOMPLETO");
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_PostulanteConsultaSuPerfil()
    {
        var usuarioId = Guid.NewGuid();
        var postulante = Postulante.Crear(
            personaId: null,
            usuarioId: usuarioId,
            numeroDocumentoIdentidad: "V-20202020",
            nombres: "Luis",
            apellidos: "Gómez",
            datosContacto: new DatosContacto("luis@correo.com", "+584121234567"),
            universidadAliadaId: Guid.NewGuid(),
            carreraId: Guid.NewGuid(),
            periodoActualId: Guid.NewGuid(),
            condicionAcademica: CondicionAcademicaIngreso.Matriculado,
            aceptaTratamientoDatos: true,
            fechaRegistro: DateTimeOffset.UtcNow);

        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(postulante);

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.GetAsync("/api/v1/postulantes/mio");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<PostulanteDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(postulante.Id);
        dto.UsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public async Task Debe_Retornar200OkConDatosSocioeconomicosYFamiliares_Cuando_PerfilEstaCompleto()
    {
        var usuarioId = Guid.NewGuid();
        var postulante = CrearPostulante(usuarioId);
        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Casado,
            17.5m,
            trabaja: true,
            Ocupacion.Empleado,
            1500m,
            [
                new DatosMiembroFamiliar(Parentesco.Pareja, 40, Ocupacion.Independiente, 800.25m),
                new DatosMiembroFamiliar(Parentesco.Hijo, 10, Ocupacion.Estudiante, 0m)
            ],
            usuarioId,
            DateTimeOffset.UtcNow);

        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(postulante);

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.GetAsync("/api/v1/postulantes/mio");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<PostulanteDto>();
        dto.Should().NotBeNull();
        dto!.EstadoCivil.Should().Be("CASADO");
        dto.PromedioActual.Should().Be(17.5m);
        dto.Trabaja.Should().BeTrue();
        dto.Ocupacion.Should().Be("EMPLEADO");
        dto.IngresoMensualPostulante.Should().Be(1500m);
        dto.NumeroIntegrantesFamilia.Should().Be(3);
        dto.IngresoFamiliarMensualTotal.Should().Be(2300.25m);
        dto.MiembrosFamiliares.Should().HaveCount(2);
        dto.MiembrosFamiliares[0].Parentesco.Should().Be("PAREJA");
        dto.MiembrosFamiliares[0].Edad.Should().Be(40);
        dto.MiembrosFamiliares[0].Ocupacion.Should().Be("INDEPENDIENTE");
        dto.MiembrosFamiliares[0].IngresoMensual.Should().Be(800.25m);
        dto.MiembrosFamiliares[1].Parentesco.Should().Be("HIJO");
        dto.EstadoPerfil.Should().Be("COMPLETO");
    }

    [Fact]
    public async Task Debe_Retornar200OkConListaFamiliarVacia_Cuando_NoDeclaraFamiliares()
    {
        var usuarioId = Guid.NewGuid();
        var postulante = CrearPostulante(usuarioId);
        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Soltero,
            15m,
            trabaja: false,
            Ocupacion.Estudiante,
            0m,
            [],
            usuarioId,
            DateTimeOffset.UtcNow);

        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(postulante);

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.GetAsync("/api/v1/postulantes/mio");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<PostulanteDto>();
        dto.Should().NotBeNull();
        dto!.MiembrosFamiliares.Should().BeEmpty();
        dto.NumeroIntegrantesFamilia.Should().Be(1);
        dto.IngresoFamiliarMensualTotal.Should().Be(0m);
        dto.EstadoPerfil.Should().Be("COMPLETO");
    }

    [Fact]
    public async Task Debe_Retornar200OkConPerfilIncompleto_Cuando_NoActualizoSocioeconomico()
    {
        var usuarioId = Guid.NewGuid();
        var postulante = CrearPostulante(usuarioId);

        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(postulante);

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.GetAsync("/api/v1/postulantes/mio");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<PostulanteDto>();
        dto.Should().NotBeNull();
        dto!.EstadoCivil.Should().BeNull();
        dto.PromedioActual.Should().BeNull();
        dto.Trabaja.Should().BeNull();
        dto.Ocupacion.Should().BeNull();
        dto.IngresoMensualPostulante.Should().BeNull();
        dto.NumeroIntegrantesFamilia.Should().BeNull();
        dto.IngresoFamiliarMensualTotal.Should().BeNull();
        dto.MiembrosFamiliares.Should().BeEmpty();
        dto.EstadoPerfil.Should().Be("INCOMPLETO");
    }

    [Fact]
    public async Task Debe_Retornar200OkYActualizarPerfil_Cuando_PutEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var postulante = CrearPostulante(usuarioId);
        var repo = Substitute.For<IPostulanteRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(postulante);
        uow.GuardarCambiosAsync(Arg.Any<CancellationToken>()).Returns(1);

        using var fabrica = CrearFabrica(repo: repo, uow: uow);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/postulantes/mio/socioeconomico",
            CrearPayloadSocioeconomico(
                miembros:
                [
                    new { parentesco = "PAREJA", edad = 40, ocupacion = "INDEPENDIENTE", ingresoMensual = 800.25m },
                    new { parentesco = "HIJO", edad = 10, ocupacion = "ESTUDIANTE", ingresoMensual = 0m }
                ]));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<PostulanteDto>();
        dto.Should().NotBeNull();
        dto!.EstadoCivil.Should().Be("CASADO");
        dto.PromedioActual.Should().Be(17.5m);
        dto.Trabaja.Should().BeTrue();
        dto.Ocupacion.Should().Be("EMPLEADO");
        dto.IngresoMensualPostulante.Should().Be(1500m);
        dto.NumeroIntegrantesFamilia.Should().Be(3);
        dto.IngresoFamiliarMensualTotal.Should().Be(2300.25m);
        dto.MiembrosFamiliares.Should().HaveCount(2);
        dto.EstadoPerfil.Should().Be("COMPLETO");
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_PutNoSuperaValidacion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/postulantes/mio/socioeconomico",
            CrearPayloadSocioeconomico(promedioActual: 25m, miembros: []));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar404NotFound_Cuando_NoExistePerfilParaActualizar()
    {
        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerParaActualizarPorUsuarioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Postulante?)null);

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/postulantes/mio/socioeconomico",
            CrearPayloadSocioeconomico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Debe_Retornar409Conflict_Cuando_HayConflictoDeConcurrencia()
    {
        var usuarioId = Guid.NewGuid();
        var repo = Substitute.For<IPostulanteRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(CrearPostulante(usuarioId));
        uow.GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExcepcionConflictoConcurrencia());

        using var fabrica = CrearFabrica(repo: repo, uow: uow);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/postulantes/mio/socioeconomico",
            CrearPayloadSocioeconomico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ExcepcionConflictoConcurrencia.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar422UnprocessableEntity_Cuando_PersistenciaRechazaReglaDeNegocio()
    {
        var usuarioId = Guid.NewGuid();
        var repo = Substitute.For<IPostulanteRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorUsuarioIdAsync(usuarioId, Arg.Any<CancellationToken>())
            .Returns(CrearPostulante(usuarioId));
        uow.GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new CondicionLaboralIncoherenteException("La condición laboral no es válida."));

        using var fabrica = CrearFabrica(repo: repo, uow: uow);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());

        var respuesta = await cliente.PutAsJsonAsync(
            "/api/v1/postulantes/mio/socioeconomico",
            CrearPayloadSocioeconomico());

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(CondicionLaboralIncoherenteException.CodigoError);
    }

    [Fact]
    public async Task Debe_UsarElUsuarioDelTokenEIgnorarElDelCuerpo_Cuando_ActualizaPerfil()
    {
        var usuarioAutenticadoId = Guid.NewGuid();
        var usuarioSuplantadoId = Guid.NewGuid();
        var postulante = CrearPostulante(usuarioAutenticadoId);
        var repo = Substitute.For<IPostulanteRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorUsuarioIdAsync(usuarioAutenticadoId, Arg.Any<CancellationToken>())
            .Returns(postulante);
        uow.GuardarCambiosAsync(Arg.Any<CancellationToken>()).Returns(1);

        using var fabrica = CrearFabrica(repo: repo, uow: uow);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioAutenticadoId.ToString());

        var json = JsonSerializer.Serialize(new
        {
            usuarioId = usuarioSuplantadoId,
            estadoCivil = "CASADO",
            promedioActual = 17.5m,
            trabaja = true,
            ocupacion = "EMPLEADO",
            ingresoMensualPostulante = 1500m,
            miembrosFamiliares = Array.Empty<object>()
        });

        using var contenido = new StringContent(json, Encoding.UTF8, "application/json");
        var respuesta = await cliente.PutAsync(
            "/api/v1/postulantes/mio/socioeconomico",
            contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await repo.Received(1)
            .ObtenerParaActualizarPorUsuarioIdAsync(usuarioAutenticadoId, Arg.Any<CancellationToken>());
        await repo.DidNotReceive()
            .ObtenerParaActualizarPorUsuarioIdAsync(usuarioSuplantadoId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_SolicitudNoSuperaValidacion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        var payloadInvalido = new
        {
            numeroDocumentoIdentidad = "",
            nombres = "",
            apellidos = "",
            datosContacto = new { correoElectronico = "no-valido", telefono = "123" },
            universidadAliadaId = Guid.Empty,
            carreraId = Guid.Empty,
            periodoAcademicoId = Guid.Empty,
            condicionAcademica = "OTRA",
            aceptaTratamientoDatos = false
        };

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/postulantes", payloadInvalido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_NoHayAutenticacion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuestaPost = await cliente.PostAsJsonAsync(
            "/api/v1/postulantes",
            CrearPayloadValido(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        var respuestaGet = await cliente.GetAsync("/api/v1/postulantes/mio");
        var respuestaPut = await cliente.PutAsJsonAsync(
            "/api/v1/postulantes/mio/socioeconomico",
            CrearPayloadSocioeconomico());

        respuestaPost.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        respuestaGet.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        respuestaPut.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_TokenAutenticadoNoTieneSub()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderAutenticadoSinSub, "true");

        var respuestaGet = await cliente.GetAsync("/api/v1/postulantes/mio");

        respuestaGet.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Debe_Retornar404NotFound_Cuando_NoExistePerfilDelUsuario()
    {
        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerPorUsuarioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Postulante?)null);

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        var respuesta = await cliente.GetAsync("/api/v1/postulantes/mio");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Debe_Retornar409Conflict_Cuando_DocumentoYaExiste()
    {
        var repo = Substitute.For<IPostulanteRepository>();
        repo.ObtenerPorUsuarioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Postulante?)null);
        repo.ExistePorNumeroDocumentoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        using var fabrica = CrearFabrica(repo: repo);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/postulantes",
            CrearPayloadValido(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Debe_Retornar422UnprocessableEntity_Cuando_UniversidadNoEsElegible()
    {
        var universidadId = Guid.NewGuid();
        var carreraId = Guid.NewGuid();
        var periodoId = Guid.NewGuid();

        var repo = Substitute.For<IPostulanteRepository>();
        var periodoClient = Substitute.For<IPeriodoAcademicoClient>();
        var catalogoClient = Substitute.For<ICatalogoAcademicoClient>();

        periodoClient.ObtenerPeriodoActualAsync(Arg.Any<CancellationToken>())
            .Returns(new PeriodoAcademicoActual(periodoId, "2026-II", EstadoPeriodoAcademico.Vigente));
        catalogoClient.ObtenerUniversidadAsync(universidadId, Arg.Any<CancellationToken>())
            .Throws(new UniversidadNoElegibleException());

        using var fabrica = CrearFabrica(repo, periodoClient, catalogoClient);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/postulantes",
            CrearPayloadValido(universidadId, carreraId, periodoId));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Debe_Retornar503ServiceUnavailable_Cuando_FallaDependenciaExterna()
    {
        var periodoClient = Substitute.For<IPeriodoAcademicoClient>();
        periodoClient.ObtenerPeriodoActualAsync(Arg.Any<CancellationToken>())
            .Throws(new DependenciaNoDisponibleException());

        using var fabrica = CrearFabrica(periodoClient: periodoClient);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.NewGuid().ToString());

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/postulantes",
            CrearPayloadValido(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        respuesta.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Debe_IgnorarUsuarioIdEnviadoEnElCuerpoYUsarElDelToken_Cuando_RegistraPostulante()
    {
        var usuarioAutenticadoId = Guid.NewGuid();
        var usuarioSuplantadoId = Guid.NewGuid();
        var universidadId = Guid.NewGuid();
        var carreraId = Guid.NewGuid();
        var periodoId = Guid.NewGuid();

        var repo = Substitute.For<IPostulanteRepository>();
        var periodoClient = Substitute.For<IPeriodoAcademicoClient>();
        var catalogoClient = Substitute.For<ICatalogoAcademicoClient>();
        var uow = Substitute.For<IUnitOfWork>();

        ConfigurarMocksValidos(repo, periodoClient, catalogoClient, periodoId, universidadId, carreraId);

        Postulante? postulanteCapturado = null;
        await repo.AgregarAsync(
            Arg.Do<Postulante>(p => postulanteCapturado = p),
            Arg.Any<CancellationToken>());

        using var fabrica = CrearFabrica(repo, periodoClient, catalogoClient, uow);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioAutenticadoId.ToString());

        var jsonConUsuarioSuplantado = JsonSerializer.Serialize(new
        {
            usuarioId = usuarioSuplantadoId,
            numeroDocumentoIdentidad = "V-30303030",
            nombres = "María",
            apellidos = "Rojas",
            datosContacto = new
            {
                correoElectronico = "maria@correo.com",
                telefono = "+584141112233"
            },
            universidadAliadaId = universidadId,
            carreraId,
            periodoAcademicoId = periodoId,
            condicionAcademica = "ADMITIDO",
            aceptaTratamientoDatos = true
        });

        using var contenido = new StringContent(jsonConUsuarioSuplantado, Encoding.UTF8, "application/json");
        var respuesta = await cliente.PostAsync("/api/v1/postulantes", contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await respuesta.Content.ReadFromJsonAsync<PostulanteDto>();
        dto.Should().NotBeNull();
        dto!.UsuarioId.Should().Be(usuarioAutenticadoId);
        dto.UsuarioId.Should().NotBe(usuarioSuplantadoId);
        postulanteCapturado.Should().NotBeNull();
        postulanteCapturado!.UsuarioId.Should().Be(usuarioAutenticadoId);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IPostulanteRepository? repo = null,
        IPeriodoAcademicoClient? periodoClient = null,
        ICatalogoAcademicoClient? catalogoClient = null,
        IUnitOfWork? uow = null)
    {
        return _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IPostulanteRepository>();
                servicios.RemoveAll<IRubricaRepository>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(repo ?? Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(periodoClient ?? Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(catalogoClient ?? Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(uow ?? Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);

                servicios.AddAuthentication(opciones =>
                    {
                        opciones.DefaultAuthenticateScheme = EsquemaPrueba;
                        opciones.DefaultChallengeScheme = EsquemaPrueba;
                        opciones.DefaultScheme = EsquemaPrueba;
                    })
                    .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionPrueba>(
                        EsquemaPrueba,
                        _ => { });
            });
        });
    }

    private static void ConfigurarMocksValidos(
        IPostulanteRepository repo,
        IPeriodoAcademicoClient periodoClient,
        ICatalogoAcademicoClient catalogoClient,
        Guid periodoId,
        Guid universidadId,
        Guid carreraId)
    {
        repo.ObtenerPorUsuarioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Postulante?)null);
        repo.ExistePorNumeroDocumentoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        periodoClient.ObtenerPeriodoActualAsync(Arg.Any<CancellationToken>())
            .Returns(new PeriodoAcademicoActual(periodoId, "2026-II", EstadoPeriodoAcademico.Vigente));

        catalogoClient.ObtenerUniversidadAsync(universidadId, Arg.Any<CancellationToken>())
            .Returns(new UniversidadAliadaCatalogo(
                universidadId,
                "Universidad Central",
                EstadoUniversidadAliada.Activa,
                TieneConvenioVigente: true,
                Carreras: [new CarreraCatalogo(carreraId, "Ingeniería", Activa: true)]));
    }

    private static object CrearPayloadValido(Guid universidadId, Guid carreraId, Guid periodoId)
        => new
        {
            numeroDocumentoIdentidad = "V-12345678",
            nombres = "Ana",
            apellidos = "Pérez",
            datosContacto = new
            {
                correoElectronico = "ana@correo.com",
                telefono = "+584141234567"
            },
            universidadAliadaId = universidadId,
            carreraId,
            periodoAcademicoId = periodoId,
            condicionAcademica = "ADMITIDO",
            aceptaTratamientoDatos = true
        };

    private static object CrearPayloadSocioeconomico(
        decimal? promedioActual = 17.5m,
        object[]? miembros = null)
        => new
        {
            estadoCivil = "CASADO",
            promedioActual,
            trabaja = true,
            ocupacion = "EMPLEADO",
            ingresoMensualPostulante = 1500m,
            miembrosFamiliares = miembros ?? []
        };

    private static Postulante CrearPostulante(Guid usuarioId)
        => Postulante.Crear(
            personaId: null,
            usuarioId,
            numeroDocumentoIdentidad: "V-20202020",
            nombres: "Luis",
            apellidos: "Gómez",
            datosContacto: new DatosContacto("luis@correo.com", "+584121234567"),
            universidadAliadaId: Guid.NewGuid(),
            carreraId: Guid.NewGuid(),
            periodoActualId: Guid.NewGuid(),
            condicionAcademica: CondicionAcademicaIngreso.Matriculado,
            aceptaTratamientoDatos: true,
            fechaRegistro: DateTimeOffset.UtcNow);

    private sealed class ManejadorAutenticacionPrueba : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ManejadorAutenticacionPrueba(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.ContainsKey(HeaderAutenticadoSinSub))
            {
                var identidadSinSub = new ClaimsIdentity([new Claim("roles", "POSTULANTE")], EsquemaPrueba);
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(identidadSinSub), EsquemaPrueba)));
            }

            if (Request.Headers.TryGetValue(HeaderSub, out var valoresSub))
            {
                var sub = valoresSub.FirstOrDefault();
                var identidad = new ClaimsIdentity(
                    [new Claim("sub", sub ?? string.Empty)],
                    EsquemaPrueba);
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}
