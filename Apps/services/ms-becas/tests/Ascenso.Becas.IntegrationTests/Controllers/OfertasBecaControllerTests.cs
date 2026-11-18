using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Idempotencia;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class OfertasBecaControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private const string EncabezadoIdempotencia = "Idempotency-Key";
    private const string EncabezadoRoles = "X-Test-Roles";
    private const string EncabezadoPostulante = "X-Test-Postulante";
    private const string EncabezadoProgramas = "X-Test-Programas";
    private const string EncabezadoAcr = "X-Test-Acr";

    private readonly OfertasBecaApiFactory _fabricaBase;

    public OfertasBecaControllerTests(OfertasBecaApiFactory fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_ExponerLasRutasDeOfertas_ConLosCodigosDeRespuesta_EnOpenApi()
    {
        using var cliente = _fabricaBase.CreateClient();

        var respuesta = await cliente.GetAsync("/openapi/v1.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var rutas = documento.RootElement.GetProperty("paths");

        VerificarOperacion(
            rutas,
            "/api/v1/becas/ofertas",
            "post",
            "201",
            "400",
            "401",
            "403",
            "409",
            "422",
            "503");

        VerificarOperacion(
            rutas,
            "/api/v1/becas/ofertas/{ofertaId}",
            "get",
            "200",
            "400",
            "401",
            "403",
            "404");

        VerificarOperacion(
            rutas,
            "/api/v1/becas/ofertas/{ofertaId}/aceptar",
            "post",
            "200",
            "400",
            "401",
            "403",
            "404",
            "409");

        VerificarOperacion(
            rutas,
            "/api/v1/becas/ofertas/{ofertaId}/rechazar",
            "post",
            "204",
            "400",
            "401",
            "403",
            "404",
            "409");

        VerificarOperacion(
            rutas,
            "/api/v1/becas/ofertas/{ofertaId}/anular",
            "post",
            "204",
            "400",
            "401",
            "403",
            "404",
            "409");
    }

    [Fact]
    public async Task Debe_EmitirOferta_Cuando_ElCoordinadorTiene2FAYAlcance()
    {
        var programaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var ofertaId = Guid.NewGuid();
        var resultadoEsperado = new OfertaEmitidaDto(
            ofertaId,
            Guid.NewGuid(),
            "PENDIENTE_RESPUESTA",
            DateTimeOffset.UtcNow.AddDays(15));

        var comandos = Substitute.For<ICommandDispatcher>();
        comandos
            .EnviarAsync<EmitirOfertaBecaCommand, OfertaEmitidaDto>(
                Arg.Any<EmitirOfertaBecaCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resultadoEsperado));

        var postulaciones = Substitute.For<IPostulacionesClient>();
        postulaciones
            .ObtenerAdjudicabilidadAsync(
                postulacionId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                CrearAdjudicabilidad(postulacionId, programaId)));

        using var fabrica = CrearFabrica(
            comandos: comandos,
            postulaciones: postulaciones);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-emision-0001");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/becas/ofertas",
            new { postulacionId });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();
        respuesta.Headers.Location!.ToString().Should().Contain(
            $"/api/v1/becas/ofertas/{ofertaId}");

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<OfertaEmitidaDto>();
        cuerpo.Should().Be(resultadoEsperado);

        await comandos.Received(1).EnviarAsync<
            EmitirOfertaBecaCommand,
            OfertaEmitidaDto>(
            Arg.Is<EmitirOfertaBecaCommand>(
                comando => comando.PostulacionId == postulacionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarEmision_Cuando_ElCoordinadorNoTieneAlcance()
    {
        var programaDelToken = Guid.NewGuid();
        var programaDeLaPostulacion = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        var postulaciones = Substitute.For<IPostulacionesClient>();
        postulaciones
            .ObtenerAdjudicabilidadAsync(
                postulacionId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                CrearAdjudicabilidad(postulacionId, programaDeLaPostulacion)));

        using var fabrica = CrearFabrica(postulaciones: postulaciones);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaDelToken);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-emision-0002");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/becas/ofertas",
            new { postulacionId });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_ElCoordinadorNoPresenta2FA()
    {
        var programaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId, con2FA: false);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-emision-0003");

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/becas/ofertas",
            new { postulacionId });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Debe_ConsultarOferta_Cuando_ElPostulanteEsDueno()
    {
        var ofertaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var oferta = CrearOferta(ofertaId, programaId);

        var consultas = Substitute.For<IQueryDispatcher>();
        consultas
            .ConsultarAsync<ObtenerOfertaQuery, OfertaBecaDto>(
                Arg.Any<ObtenerOfertaQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(oferta));

        var consultasBeca = Substitute.For<IConsultasBeca>();
        consultasBeca
            .ObtenerRecursoOfertaAsync(
                ofertaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoOfertaDto?>(
                new RecursoOfertaDto(
                    ofertaId,
                    oferta.BecaId,
                    programaId,
                    postulanteId)));

        using var fabrica = CrearFabrica(
            consultas: consultas,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, postulanteId);

        var respuesta = await cliente.GetAsync($"/api/v1/becas/ofertas/{ofertaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<OfertaBecaDto>();
        cuerpo.Should().Be(oferta);
    }

    [Fact]
    public async Task Debe_ConsultarOferta_Cuando_ElCoordinadorTieneAlcance()
    {
        var ofertaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var oferta = CrearOferta(ofertaId, programaId);

        var consultas = Substitute.For<IQueryDispatcher>();
        consultas
            .ConsultarAsync<ObtenerOfertaQuery, OfertaBecaDto>(
                Arg.Any<ObtenerOfertaQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(oferta));

        var consultasBeca = Substitute.For<IConsultasBeca>();
        consultasBeca
            .ObtenerRecursoOfertaAsync(
                ofertaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoOfertaDto?>(
                new RecursoOfertaDto(
                    ofertaId,
                    oferta.BecaId,
                    programaId,
                    Guid.NewGuid())));

        using var fabrica = CrearFabrica(
            consultas: consultas,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId);

        var respuesta = await cliente.GetAsync($"/api/v1/becas/ofertas/{ofertaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElPostulanteConsultaUnaOfertaAjena()
    {
        var ofertaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var duenoId = Guid.NewGuid();

        var consultasBeca = Substitute.For<IConsultasBeca>();
        consultasBeca
            .ObtenerRecursoOfertaAsync(
                ofertaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoOfertaDto?>(
                new RecursoOfertaDto(
                    ofertaId,
                    Guid.NewGuid(),
                    programaId,
                    duenoId)));

        using var fabrica = CrearFabrica(consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, Guid.NewGuid());

        var respuesta = await cliente.GetAsync($"/api/v1/becas/ofertas/{ofertaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Debe_Responder404_Cuando_LaOfertaNoExiste()
    {
        var ofertaId = Guid.NewGuid();

        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, Guid.NewGuid());

        var respuesta = await cliente.GetAsync($"/api/v1/becas/ofertas/{ofertaId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Debe_AceptarOferta_Cuando_ElPostulanteEsDueno()
    {
        var ofertaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var resultadoEsperado = new OfertaRespondidaDto(
            ofertaId,
            Guid.NewGuid(),
            "ACEPTADA",
            "ACTIVA");

        var comandos = Substitute.For<ICommandDispatcher>();
        comandos
            .EnviarAsync<AceptarOfertaBecaCommand, OfertaRespondidaDto>(
                Arg.Any<AceptarOfertaBecaCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resultadoEsperado));

        var consultasBeca = CrearConsultasBeca(
            ofertaId,
            programaId,
            postulanteId);

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, postulanteId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-aceptar-0001");

        var respuesta = await cliente.PostAsync(
            $"/api/v1/becas/ofertas/{ofertaId}/aceptar",
            content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<OfertaRespondidaDto>();
        cuerpo.Should().Be(resultadoEsperado);

        await comandos.Received(1)
            .EnviarAsync<AceptarOfertaBecaCommand, OfertaRespondidaDto>(
                Arg.Is<AceptarOfertaBecaCommand>(
                    comando => comando.OfertaId == ofertaId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarOferta_Cuando_ElPostulanteEsDueno()
    {
        var ofertaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var motivo = "No puedo continuar con el programa.";

        var comandos = Substitute.For<ICommandDispatcher>();
        var consultasBeca = CrearConsultasBeca(
            ofertaId,
            programaId,
            postulanteId);

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoPostulante(cliente, postulanteId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-rechazar-0001");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/ofertas/{ofertaId}/rechazar",
            new { motivo });

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await comandos.Received(1).EnviarAsync<RechazarOfertaBecaCommand>(
            Arg.Is<RechazarOfertaBecaCommand>(
                comando => comando.OfertaId == ofertaId
                    && comando.Motivo == motivo),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_AnularOferta_Cuando_ElCoordinadorTiene2FAYAlcance()
    {
        var ofertaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var justificacion = "Incumplimiento de los requisitos del programa.";

        var comandos = Substitute.For<ICommandDispatcher>();
        var consultasBeca = CrearConsultasBeca(
            ofertaId,
            programaId,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-anular-0001");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/ofertas/{ofertaId}/anular",
            new { justificacion });

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await comandos.Received(1).EnviarAsync<AnularOfertaBecaCommand>(
            Arg.Is<AnularOfertaBecaCommand>(
                comando => comando.OfertaId == ofertaId
                    && comando.Justificacion == justificacion),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElCoordinadorAnulaSinAlcance()
    {
        var ofertaId = Guid.NewGuid();
        var programaDeLaOferta = Guid.NewGuid();
        var programaDelToken = Guid.NewGuid();

        var consultasBeca = CrearConsultasBeca(
            ofertaId,
            programaDeLaOferta,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaDelToken);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-anular-0002");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/ofertas/{ofertaId}/anular",
            new { justificacion = "Justificación suficientemente larga." });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_NoHayAutenticacion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(
            $"/api/v1/becas/ofertas/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        ICommandDispatcher? comandos = null,
        IQueryDispatcher? consultas = null,
        IConsultasBeca? consultasBeca = null,
        IPostulacionesClient? postulaciones = null)
        => _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<ICommandDispatcher>();
                servicios.AddSingleton(
                    comandos ?? Substitute.For<ICommandDispatcher>());

                servicios.RemoveAll<IQueryDispatcher>();
                servicios.AddSingleton(
                    consultas ?? Substitute.For<IQueryDispatcher>());

                servicios.RemoveAll<IConsultasBeca>();
                servicios.AddSingleton(
                    consultasBeca ?? Substitute.For<IConsultasBeca>());

                servicios.RemoveAll<IPostulacionesClient>();
                servicios.AddSingleton(
                    postulaciones ?? Substitute.For<IPostulacionesClient>());
            });
        });

    private static IConsultasBeca CrearConsultasBeca(
        Guid ofertaId,
        Guid programaId,
        Guid postulanteId)
    {
        var consultas = Substitute.For<IConsultasBeca>();
        consultas
            .ObtenerRecursoOfertaAsync(
                ofertaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoOfertaDto?>(
                new RecursoOfertaDto(
                    ofertaId,
                    Guid.NewGuid(),
                    programaId,
                    postulanteId)));

        return consultas;
    }

    private static OfertaBecaDto CrearOferta(Guid ofertaId, Guid programaId)
        => new(
            ofertaId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            programaId,
            "Beca de excelencia",
            Guid.NewGuid(),
            "PENDIENTE_RESPUESTA",
            1500m,
            15000m,
            10,
            new CondicionesPermanenciaDto(
                14m,
                4,
                1,
                20,
                "REVOCACION"),
            "https://archivos.example.com/carta.pdf",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(15),
            null,
            null);

    private static ResultadoAdjudicabilidad CrearAdjudicabilidad(
        Guid postulacionId,
        Guid programaId)
        => new(
            postulacionId,
            true,
            programaId,
            Guid.NewGuid(),
            "2026-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Estudiante de prueba",
            "V-12345678",
            Guid.NewGuid());

    private static void AutenticarComoPostulante(
        HttpClient cliente,
        Guid postulanteId)
    {
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "POSTULANTE");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoPostulante,
            postulanteId.ToString());
    }

    private static void AutenticarComoCoordinador(
        HttpClient cliente,
        Guid programaId,
        bool con2FA = true)
    {
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "COORDINADOR_PROGRAMA");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoProgramas,
            programaId.ToString());

        if (con2FA)
        {
            cliente.DefaultRequestHeaders.Add(
                EncabezadoAcr,
                "urn:ascenso:loa:2fa");
        }
    }

    private static void VerificarOperacion(
        JsonElement rutas,
        string ruta,
        string metodo,
        params string[] codigos)
    {
        rutas.TryGetProperty(ruta, out var operaciones).Should().BeTrue(
            $"OpenAPI debe exponer la ruta {ruta}.");

        operaciones.TryGetProperty(metodo, out var operacion).Should().BeTrue(
            $"OpenAPI debe exponer el método {metodo} para {ruta}.");

        operacion.TryGetProperty("responses", out var respuestas).Should().BeTrue(
            $"OpenAPI debe declarar respuestas para {metodo} {ruta}.");

        foreach (var codigo in codigos)
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue(
                $"OpenAPI debe declarar el código {codigo} para {metodo} {ruta}.");
        }
    }
}

/// <summary>
/// Fábrica de pruebas con autenticación simulada, almacén de idempotencia en
/// memoria y sin servidores en segundo plano que dependan de infraestructura.
/// </summary>
public sealed class OfertasBecaApiFactory : WebApplicationFactory<Program>
{
    private const string EsquemaPrueba = "TestJwt";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Becas"] =
                        "Host=localhost;Port=5432;Database=ascenso_becas_tests;Username=postgres;Password=postgres",
                    ["Mensajeria:Habilitada"] = "false"
                });
        });

        builder.ConfigureTestServices(servicios =>
        {
            EliminarServidoresHangfire(servicios);

            servicios
                .AddAuthentication(opciones =>
                {
                    opciones.DefaultAuthenticateScheme = EsquemaPrueba;
                    opciones.DefaultChallengeScheme = EsquemaPrueba;
                    opciones.DefaultScheme = EsquemaPrueba;
                })
                .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionPrueba>(
                    EsquemaPrueba,
                    _ => { });

            var almacen = Substitute.For<IAlmacenIdempotencia>();
            almacen
                .IntentarRegistrarAsync(
                    Arg.Any<RegistroIdempotencia>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            servicios.RemoveAll<IAlmacenIdempotencia>();
            servicios.AddSingleton(almacen);

            // Los adaptadores de PDF y almacenamiento se construyen en tareas
            // posteriores; aquí solo se satisface la validación del contenedor.
            servicios.RemoveAll<ICartaOfertaPdfGenerator>();
            servicios.AddSingleton(Substitute.For<ICartaOfertaPdfGenerator>());

            servicios.RemoveAll<IAlmacenamientoArchivos>();
            servicios.AddSingleton(Substitute.For<IAlmacenamientoArchivos>());

            servicios.RemoveAll<IProgramadorExpiracionOfertas>();
            servicios.AddSingleton(Substitute.For<IProgramadorExpiracionOfertas>());
        });
    }

    private static void EliminarServidoresHangfire(IServiceCollection servicios)
    {
        // Hangfire registra BackgroundJobServerHostedService con una factoría,
        // por lo que ImplementationType es null y el filtro por tipo no lo
        // detecta. Se revisa también el tipo que declara la factoría.
        var descriptores = servicios
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && (EsDeHangfire(descriptor.ImplementationType)
                    || EsDeHangfire(
                        descriptor.ImplementationFactory?.Method.DeclaringType)))
            .ToList();

        foreach (var descriptor in descriptores)
        {
            servicios.Remove(descriptor);
        }

        static bool EsDeHangfire(Type? tipo)
            => tipo?.FullName?.Contains(
                "Hangfire",
                StringComparison.OrdinalIgnoreCase) == true;
    }

    private sealed class ManejadorAutenticacionPrueba
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ManejadorAutenticacionPrueba(
            IOptionsMonitor<AuthenticationSchemeOptions> opciones,
            ILoggerFactory registro,
            UrlEncoder codificador)
            : base(opciones, registro, codificador)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-Test-Roles")
                && !Request.Headers.ContainsKey("X-Test-Postulante")
                && !Request.Headers.ContainsKey("X-Test-Cliente"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new("sub", Guid.NewGuid().ToString())
            };

            foreach (var rol in ObtenerValores("X-Test-Roles"))
            {
                claims.Add(new Claim("roles", rol));
            }

            foreach (var programa in ObtenerValores("X-Test-Programas"))
            {
                claims.Add(new Claim("programa_ids", programa));
            }

            var postulante = Request.Headers["X-Test-Postulante"].ToString();
            if (!string.IsNullOrWhiteSpace(postulante))
            {
                claims.Add(new Claim("postulante_id", postulante));
            }

            var acr = Request.Headers["X-Test-Acr"].ToString();
            if (!string.IsNullOrWhiteSpace(acr))
            {
                claims.Add(new Claim("acr", acr));
            }

            var cliente = Request.Headers["X-Test-Cliente"].ToString();
            if (!string.IsNullOrWhiteSpace(cliente))
            {
                claims.Add(new Claim("client_id", cliente));
            }

            foreach (var alcance in ObtenerValores("X-Test-Scopes"))
            {
                claims.Add(new Claim("scope", alcance));
            }

            var identidad = new ClaimsIdentity(
                claims,
                EsquemaPrueba,
                nameType: "sub",
                roleType: "roles");

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(
                    new ClaimsPrincipal(identidad),
                    EsquemaPrueba)));
        }

        private string[] ObtenerValores(string encabezado)
            => Request.Headers[encabezado]
                .ToString()
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries);
    }
}
