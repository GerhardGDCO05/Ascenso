using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Comites.DTOs;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Controllers;

public sealed class ComitesEvaluadoresControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolAdminFinanciero = "ADMIN_FINANCIERO";
    private const string RolMiembroComite = "MIEMBRO_COMITE";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public ComitesEvaluadoresControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_Retornar201CreatedLocationYJefaturaComoPrimerMiembro_Cuando_CrearComiteEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var jefaturaId = Guid.NewGuid();
        var repo = Substitute.For<IComiteEvaluadorRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        repo.ExistePorProgramaYConvocatoriaAsync(programaId, convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/comites-evaluadores",
            new
            {
                programaBecaId = programaId,
                convocatoriaId,
                nombre = "Comité Evaluador Beca Excelencia 2026-II",
                jefaturaUsuarioId = jefaturaId
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();

        var dto = await respuesta.Content.ReadFromJsonAsync<ComiteEvaluadorDto>();
        dto.Should().NotBeNull();
        dto!.ProgramaBecaId.Should().Be(programaId);
        dto.ConvocatoriaId.Should().Be(convocatoriaId);
        dto.Nombre.Should().Be("Comité Evaluador Beca Excelencia 2026-II");
        dto.JefaturaUsuarioId.Should().Be(jefaturaId);
        dto.ConformadoPorUsuarioId.Should().Be(usuarioId);
        dto.Activo.Should().BeTrue();
        dto.Version.Should().Be(1);
        dto.Miembros.Should().ContainSingle();
        dto.Miembros[0].UsuarioEvaluadorId.Should().Be(jefaturaId);
        dto.Miembros[0].Tipo.Should().Be("INTERNO");
        dto.Miembros[0].Rol.Should().Be("JEFATURA");
        dto.Miembros[0].Activo.Should().BeTrue();
        dto.Miembros[0].FechaDesvinculacion.Should().BeNull();
        respuesta.Headers.Location!.ToString().Should().Contain($"/api/v1/comites-evaluadores/{dto.Id}");
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_CrearComiteEsInvalido()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/comites-evaluadores",
            new
            {
                programaBecaId = Guid.Empty,
                convocatoriaId = Guid.Empty,
                nombre = "",
                jefaturaUsuarioId = Guid.Empty
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar409ConflictConComiteDuplicado_Cuando_YaExisteComiteParaProgramaYConvocatoria()
    {
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var repo = Substitute.For<IComiteEvaluadorRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ExistePorProgramaYConvocatoriaAsync(programaId, convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(true);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/comites-evaluadores",
            new
            {
                programaBecaId = programaId,
                convocatoriaId,
                nombre = "Comité Duplicado",
                jefaturaUsuarioId = Guid.NewGuid()
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, ComiteDuplicadoException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200OkConCodigosEnMayusculas_Cuando_ObtenerComitePorIdExiste()
    {
        var usuarioId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Consulta",
            Guid.NewGuid(),
            ahora,
            usuarioId);
        var miembroInterno = comite.AgregarMiembro(Guid.NewGuid(), TipoEvaluador.Interno, ahora.AddMinutes(1), usuarioId);
        comite.AgregarMiembro(Guid.NewGuid(), TipoEvaluador.Externo, ahora.AddMinutes(2), usuarioId);
        comite.DesvincularMiembro(miembroInterno.Id, ahora.AddMinutes(3), usuarioId);

        var repo = Substitute.For<IComiteEvaluadorRepository>();
        repo.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.GetAsync($"/api/v1/comites-evaluadores/{comite.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var miembrosJson = doc.RootElement.GetProperty("miembros");
        miembrosJson.GetArrayLength().Should().Be(3);

        var dto = await respuesta.Content.ReadFromJsonAsync<ComiteEvaluadorDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(comite.Id);
        dto.Miembros.Should().Contain(m => m.Rol == "JEFATURA" && m.Tipo == "INTERNO" && m.Activo);
        dto.Miembros.Should().Contain(m => m.Id == miembroInterno.Id && m.Rol == "EVALUADOR" && m.Tipo == "INTERNO" && !m.Activo && m.FechaDesvinculacion.HasValue);
        dto.Miembros.Should().Contain(m => m.Rol == "EVALUADOR" && m.Tipo == "EXTERNO" && m.Activo);
    }

    [Fact]
    public async Task Debe_Retornar404NotFoundConComiteNoEncontrado_Cuando_ObtenerComitePorIdNoExiste()
    {
        var repo = Substitute.For<IComiteEvaluadorRepository>();
        repo.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ComiteEvaluador?)null);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.GetAsync($"/api/v1/comites-evaluadores/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, ComiteNoEncontradoException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_ObtenerComitePorProgramaYConvocatoriaExiste()
    {
        var usuarioId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            programaId,
            convocatoriaId,
            "Comité Programa",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            usuarioId);

        var repo = Substitute.For<IComiteEvaluadorRepository>();
        repo.ObtenerPorProgramaYConvocatoriaAsync(programaId, convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/comites-evaluadores/programas/{programaId}/convocatorias/{convocatoriaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<ComiteEvaluadorDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(comite.Id);
        dto.ProgramaBecaId.Should().Be(programaId);
        dto.ConvocatoriaId.Should().Be(convocatoriaId);
    }

    [Fact]
    public async Task Debe_Retornar404NotFoundConComiteNoEncontrado_Cuando_ObtenerComitePorProgramaYConvocatoriaNoExiste()
    {
        var repo = Substitute.For<IComiteEvaluadorRepository>();
        repo.ObtenerPorProgramaYConvocatoriaAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ComiteEvaluador?)null);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/comites-evaluadores/programas/{Guid.NewGuid()}/convocatorias/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, ComiteNoEncontradoException.CodigoError);
    }

    [Theory]
    [InlineData("INTERNO")]
    [InlineData("EXTERNO")]
    public async Task Debe_Retornar201Created_Cuando_AgregarMiembroInternoOExternoEsValido(string tipo)
    {
        var usuarioId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Miembros",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            usuarioId);

        var repo = Substitute.For<IComiteEvaluadorRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros",
            new
            {
                usuarioEvaluadorId = evaluadorId,
                tipo
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await respuesta.Content.ReadFromJsonAsync<ComiteEvaluadorDto>();
        dto.Should().NotBeNull();
        dto!.Miembros.Should().HaveCount(2);
        dto.Miembros.Should().Contain(m =>
            m.UsuarioEvaluadorId == evaluadorId
            && m.Tipo == tipo
            && m.Rol == "EVALUADOR"
            && m.Activo);
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400ConSolicitudInvalida_Cuando_AgregarMiembroTieneTipoInvalido()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/comites-evaluadores/{Guid.NewGuid()}/miembros",
            new
            {
                usuarioEvaluadorId = Guid.NewGuid(),
                tipo = "INVITADO"
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await VerificarCodigoErrorAsync(respuesta, "SOLICITUD_INVALIDA");
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_AgregarMiembroTieneUsuarioEvaluadorIdVacio()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/comites-evaluadores/{Guid.NewGuid()}/miembros",
            new
            {
                usuarioEvaluadorId = Guid.Empty,
                tipo = "INTERNO"
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar404NotFoundConComiteNoEncontrado_Cuando_AgregarMiembroAComiteInexistente()
    {
        var repo = Substitute.For<IComiteEvaluadorRepository>();
        repo.ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ComiteEvaluador?)null);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/comites-evaluadores/{Guid.NewGuid()}/miembros",
            new
            {
                usuarioEvaluadorId = Guid.NewGuid(),
                tipo = "INTERNO"
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, ComiteNoEncontradoException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar409ConflictConMiembroComiteDuplicado_Cuando_UsuarioYaEstaActivoEnElComite()
    {
        var usuarioId = Guid.NewGuid();
        var jefaturaId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Duplicado",
            jefaturaId,
            DateTimeOffset.UtcNow,
            usuarioId);

        var repo = Substitute.For<IComiteEvaluadorRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros",
            new
            {
                usuarioEvaluadorId = jefaturaId,
                tipo = "INTERNO"
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, MiembroComiteDuplicadoException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200OkYSerIdempotenteEnSegundaDesvinculacion_Cuando_DesvincularMiembroEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Desvinculación",
            Guid.NewGuid(),
            ahora,
            usuarioId);
        var miembro = comite.AgregarMiembro(Guid.NewGuid(), TipoEvaluador.Externo, ahora.AddMinutes(1), usuarioId);

        var repo = Substitute.For<IComiteEvaluadorRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var primera = await cliente.DeleteAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros/{miembro.Id}");

        primera.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoPrimera = await primera.Content.ReadFromJsonAsync<ComiteEvaluadorDto>();
        dtoPrimera.Should().NotBeNull();
        var miembroDto1 = dtoPrimera!.Miembros.Single(m => m.Id == miembro.Id);
        miembroDto1.Activo.Should().BeFalse();
        miembroDto1.FechaDesvinculacion.Should().NotBeNull();
        var versionTrasPrimera = dtoPrimera.Version;

        var segunda = await cliente.DeleteAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros/{miembro.Id}");

        segunda.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoSegunda = await segunda.Content.ReadFromJsonAsync<ComiteEvaluadorDto>();
        dtoSegunda.Should().NotBeNull();
        dtoSegunda!.Version.Should().Be(versionTrasPrimera);
        var miembroDto2 = dtoSegunda.Miembros.Single(m => m.Id == miembro.Id);
        miembroDto2.Activo.Should().BeFalse();
        miembroDto2.FechaDesvinculacion.Should().Be(miembroDto1.FechaDesvinculacion);

        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar422ConJefaturaNoDesvinculable_Cuando_SeIntentaDesvincularLaJefatura()
    {
        var usuarioId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Jefatura",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            usuarioId);
        var miembroJefatura = comite.Miembros.Single();

        var repo = Substitute.For<IComiteEvaluadorRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.DeleteAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros/{miembroJefatura.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respuesta, JefaturaNoDesvinculableException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar404ConMiembroComiteNoEncontrado_Cuando_MiembroNoPerteneceAlComite()
    {
        var usuarioId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Miembro Inexistente",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            usuarioId);

        var repo = Substitute.For<IComiteEvaluadorRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.DeleteAsync(
            $"/api/v1/comites-evaluadores/{comite.Id}/miembros/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, MiembroComiteNoEncontradoException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(RolAdminFinanciero)]
    [InlineData(RolMiembroComite)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_CrearOConsultarComiteConRolNoAutorizado(string rol)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), rol);

        var respCrear = await cliente.PostAsJsonAsync(
            "/api/v1/comites-evaluadores",
            new
            {
                programaBecaId = Guid.NewGuid(),
                convocatoriaId = Guid.NewGuid(),
                nombre = "Comité",
                jefaturaUsuarioId = Guid.NewGuid()
            });
        await VerificarPermisoDenegadoAsync(respCrear);

        var respObtener = await cliente.GetAsync($"/api/v1/comites-evaluadores/{Guid.NewGuid()}");
        await VerificarPermisoDenegadoAsync(respObtener);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IComiteEvaluadorRepository? repo = null,
        IUnitOfWork? uow = null)
    {
        return _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IPostulanteRepository>();
                servicios.RemoveAll<IRubricaRepository>();
                servicios.RemoveAll<IComiteEvaluadorRepository>();
                servicios.RemoveAll<IAsignacionEvaluacionRepository>();
                servicios.RemoveAll<IExcepcionEvaluadorUnicoRepository>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(repo ?? Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
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

    private static HttpClient CrearClienteAutenticado(
        WebApplicationFactory<Program> fabrica,
        Guid usuarioId,
        string rol)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);
        return cliente;
    }

    private static async Task VerificarPermisoDenegadoAsync(HttpResponseMessage respuesta)
    {
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerificarCodigoErrorAsync(respuesta, ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
    }

    private static async Task VerificarCodigoErrorAsync(HttpResponseMessage respuesta, string codigoEsperado)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("codigo").GetString().Should().Be(codigoEsperado);
    }

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
            if (!Request.Headers.TryGetValue(HeaderSub, out var valoresSub))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new("sub", valoresSub.FirstOrDefault() ?? string.Empty)
            };

            if (Request.Headers.TryGetValue(HeaderRol, out var valoresRol))
            {
                foreach (var rol in valoresRol.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("roles", rol!));
                }
            }

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
