using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
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

public sealed class ExcepcionEvaluadorUnicoControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public ExcepcionEvaluadorUnicoControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_Retornar201CreatedConLocationYAutorDelToken_Cuando_CoordinadorAutorizaExcepcion()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var repoExcepciones = Substitute.For<IExcepcionEvaluadorUnicoRepository>();
        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        repoExcepciones.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns((AutorizacionExcepcionEvaluadorUnico?)null);
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        using var fabrica = CrearFabrica(
            repoExcepciones: repoExcepciones,
            repoAsignaciones: repoAsignaciones,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico",
            new
            {
                justificacion = "  Único especialista disponible en inteligencia artificial aplicada.  "
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();
        respuesta.Headers.Location!.ToString()
            .Should().Contain($"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico");

        var dto = await respuesta.Content.ReadFromJsonAsync<ExcepcionEvaluadorUnicoDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().NotBeEmpty();
        dto.PostulacionId.Should().Be(postulacionId);
        dto.Justificacion.Should().Be("Único especialista disponible en inteligencia artificial aplicada.");
        dto.UsuarioAutorizadorId.Should().Be(coordinadorId);
        dto.FechaAutorizacion.Offset.Should().Be(TimeSpan.Zero);

        await repoExcepciones.Received(1)
            .AgregarAsync(Arg.Any<AutorizacionExcepcionEvaluadorUnico>(), Arg.Any<CancellationToken>());
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_ExcepcionEvaluadorUnicoExiste()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var fechaAutorizacion = new DateTimeOffset(2026, 11, 24, 18, 30, 0, TimeSpan.Zero);
        var autorizacion = AutorizacionExcepcionEvaluadorUnico.Crear(
            postulacionId,
            "Solo un evaluador externo disponible en la especialidad requerida.",
            fechaAutorizacion,
            coordinadorId,
            Guid.NewGuid());

        var repoExcepciones = Substitute.For<IExcepcionEvaluadorUnicoRepository>();
        repoExcepciones.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(autorizacion);

        using var fabrica = CrearFabrica(repoExcepciones: repoExcepciones);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<ExcepcionEvaluadorUnicoDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(autorizacion.Id);
        dto.PostulacionId.Should().Be(postulacionId);
        dto.Justificacion.Should().Be("Solo un evaluador externo disponible en la especialidad requerida.");
        dto.UsuarioAutorizadorId.Should().Be(coordinadorId);
        dto.FechaAutorizacion.Should().Be(fechaAutorizacion);
    }

    [Fact]
    public async Task Debe_Retornar404ConExcepcionNoEncontrada_Cuando_PostulacionNoTieneExcepcion()
    {
        var postulacionId = Guid.NewGuid();
        var repoExcepciones = Substitute.For<IExcepcionEvaluadorUnicoRepository>();
        repoExcepciones.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns((AutorizacionExcepcionEvaluadorUnico?)null);

        using var fabrica = CrearFabrica(repoExcepciones: repoExcepciones);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, ExcepcionEvaluadorUnicoNoEncontradaException.CodigoError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("123456789")]
    public async Task Debe_Retornar400ConJustificacionRequerida_Cuando_JustificacionEsInvalida(string justificacion)
    {
        var uow = Substitute.For<IUnitOfWork>();
        using var fabrica = CrearFabrica(uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{Guid.NewGuid()}/excepcion-evaluador-unico",
            new { justificacion });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await VerificarCodigoErrorAsync(respuesta, JustificacionExcepcionInvalidaException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_PostulacionIdEsVacioOJustificacionSuperaMaximo()
    {
        var uow = Substitute.For<IUnitOfWork>();
        using var fabrica = CrearFabrica(uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respPostIdVacio = await cliente.PostAsJsonAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{Guid.Empty}/excepcion-evaluador-unico",
            new { justificacion = "Justificación válida con suficiente longitud." });
        respPostIdVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respGetIdVacio = await cliente.GetAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{Guid.Empty}/excepcion-evaluador-unico");
        respGetIdVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respJustificacionLarga = await cliente.PostAsJsonAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{Guid.NewGuid()}/excepcion-evaluador-unico",
            new { justificacion = new string('x', 1001) });
        respJustificacionLarga.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await VerificarCodigoErrorAsync(respJustificacionLarga, JustificacionExcepcionInvalidaException.CodigoError);

        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar409ConExcepcionYaAutorizada_Cuando_PostulacionYaTieneExcepcion()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var existente = AutorizacionExcepcionEvaluadorUnico.Crear(
            postulacionId,
            "Justificación previa registrada para la postulación.",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            coordinadorId,
            Guid.NewGuid());

        var repoExcepciones = Substitute.For<IExcepcionEvaluadorUnicoRepository>();
        repoExcepciones.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(existente);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoExcepciones: repoExcepciones,
            repoAsignaciones: repoAsignaciones,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico",
            new { justificacion = "Nuevo intento de autorizar excepción duplicada." });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, ExcepcionEvaluadorUnicoYaAutorizadaException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar422ConExcepcionNoAplicable_Cuando_PostulacionYaTieneDosAsignacionesVigentes()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Con Dos Vigentes",
            Guid.NewGuid(),
            ahora.AddHours(-1),
            coordinadorId);
        var miembro1 = comite.Miembros.First();
        var miembro2 = comite.AgregarMiembro(Guid.NewGuid(), TipoEvaluador.Externo, ahora.AddMinutes(-30), coordinadorId);

        var asignacion1 = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            postulacionId,
            ahora.AddMinutes(-10),
            ahora.AddDays(5),
            coordinadorId);
        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddMinutes(-9),
            ahora.AddDays(5),
            coordinadorId);

        var repoExcepciones = Substitute.For<IExcepcionEvaluadorUnicoRepository>();
        repoExcepciones.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns((AutorizacionExcepcionEvaluadorUnico?)null);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([asignacion1, asignacion2]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoExcepciones: repoExcepciones,
            repoAsignaciones: repoAsignaciones,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico",
            new { justificacion = "Intento de excepción cuando ya existen dos evaluadores vigentes." });

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respuesta, ExcepcionEvaluadorUnicoNoAplicableException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarUnEvaluadorSinExcepcionYPermitirUnEvaluadorTrasAutorizarExcepcion_Cuando_SeEjecutaFlujoCompleto()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluadorUnicoId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Flujo Excepción",
            evaluadorUnicoId,
            ahora.AddHours(-1),
            coordinadorId);

        AutorizacionExcepcionEvaluadorUnico? excepcionPersistida = null;

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        var repoExcepciones = Substitute.For<IExcepcionEvaluadorUnicoRepository>();
        repoExcepciones.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(_ => excepcionPersistida);
        repoExcepciones.AgregarAsync(
                Arg.Do<AutorizacionExcepcionEvaluadorUnico>(a => excepcionPersistida = a),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoComites: repoComites,
            repoAsignaciones: repoAsignaciones,
            repoExcepciones: repoExcepciones,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        // 1. Sin excepción: asignar 1 evaluador responde 422 EVALUACIONES_INSUFICIENTES
        var respSinExcepcion = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(7),
                usuariosEvaluadoresIds = new[] { evaluadorUnicoId }
            });

        respSinExcepcion.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respSinExcepcion, EvaluadoresInsuficientesException.CodigoError);

        // 2. Autorizar excepción: responde 201 Created
        var respAutorizar = await cliente.PostAsJsonAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico",
            new
            {
                justificacion = "El comité solo cuenta con un especialista activo en esta línea de investigación."
            });

        respAutorizar.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Con excepción autorizada: asignar 1 evaluador responde 201 Created con código PENDIENTE en mayúsculas
        var respConExcepcion = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(7),
                usuariosEvaluadoresIds = new[] { evaluadorUnicoId }
            });

        respConExcepcion.StatusCode.Should().Be(HttpStatusCode.Created);

        var jsonAsignacion = await respConExcepcion.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonAsignacion);
        var asignacionesJson = doc.RootElement.GetProperty("asignaciones");
        asignacionesJson.GetArrayLength().Should().Be(1);
        asignacionesJson[0].GetProperty("estado").GetString().Should().Be("PENDIENTE");

        var dtoAsignacion = await respConExcepcion.Content.ReadFromJsonAsync<AsignacionesPostulacionDto>();
        dtoAsignacion.Should().NotBeNull();
        dtoAsignacion!.PostulacionId.Should().Be(postulacionId);
        dtoAsignacion.Asignaciones.Should().ContainSingle();
        dtoAsignacion.Asignaciones[0].UsuarioEvaluadorId.Should().Be(evaluadorUnicoId);
        dtoAsignacion.Asignaciones[0].Estado.Should().Be("PENDIENTE");
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IComiteEvaluadorRepository? repoComites = null,
        IAsignacionEvaluacionRepository? repoAsignaciones = null,
        IExcepcionEvaluadorUnicoRepository? repoExcepciones = null,
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
                servicios.RemoveAll<IEvaluacionRepository>();
                servicios.RemoveAll<IConsultaAsignacionParaEvaluacionRepository>();
                servicios.RemoveAll<IConsultaRubricaParaEvaluacionRepository>();
                servicios.RemoveAll<IConsultaEvaluacionesCoevaluadoresRepository>();
                servicios.RemoveAll<IResultadoConsolidadoRepository>();
                servicios.RemoveAll<IConsultaDatosConsolidacionRepository>();
                servicios.RemoveAll<IProveedorParametrosConsolidacion>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IConvocatoriaEvaluacionClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(repoComites ?? Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(repoAsignaciones ?? Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(repoExcepciones ?? Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
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
