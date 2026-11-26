using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Programas.Api.Configuracion;
using Ascenso.Programas.Api.Controllers.v1;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.ObjetosValor;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;
using Ascenso.Programas.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Ascenso.Programas.IntegrationTests.Api;

public sealed class ConvocatoriasApiIntegrationTests : IClassFixture<ConvocatoriasApiFixture>
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000701");
    private readonly ConvocatoriasApiFixture _fixture;

    public ConvocatoriasApiIntegrationTests(ConvocatoriasApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_ExigirAutenticacionYPermisoParaLosEndpointsPublicosEInternos()
    {
        using var cliente = _fixture.CrearCliente();
        var rutasProtegidas = new[]
        {
            (HttpMethod.Post, "/api/v1/convocatorias", "{}"),
            (HttpMethod.Get, "/api/v1/convocatorias", null),
            (HttpMethod.Get, $"/api/v1/convocatorias/{Guid.NewGuid():D}", null),
            (HttpMethod.Put, $"/api/v1/convocatorias/{Guid.NewGuid():D}", "{}"),
            (HttpMethod.Post, $"/api/v1/convocatorias/{Guid.NewGuid():D}/publicar", "{}"),
            (HttpMethod.Post, $"/api/v1/convocatorias/{Guid.NewGuid():D}/documentos-requeridos", "{}"),
            (HttpMethod.Delete, $"/api/v1/convocatorias/{Guid.NewGuid():D}/documentos-requeridos/{Guid.NewGuid():D}", null),
            (HttpMethod.Get, $"/internal/v1/convocatorias/{Guid.NewGuid():D}", null),
            (HttpMethod.Get, "/internal/v1/convocatorias/abiertas", null)
        };

        foreach (var (metodo, ruta, cuerpo) in rutasProtegidas)
        {
            using var solicitudSinToken = CrearSolicitud(
                metodo,
                ruta,
                cuerpo: cuerpo,
                incluirAutenticacion: false);
            var respuestaSinToken = await cliente.SendAsync(solicitudSinToken);
            respuestaSinToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized, ruta);

            using var solicitudSinPermiso = CrearSolicitud(
                metodo,
                ruta,
                permiso: "otro:permiso",
                cuerpo: cuerpo);
            var respuestaSinPermiso = await cliente.SendAsync(solicitudSinPermiso);
            respuestaSinPermiso.StatusCode.Should().Be(HttpStatusCode.Forbidden, ruta);

            using var documentoError = await JsonDocument.ParseAsync(
                await respuestaSinPermiso.Content.ReadAsStreamAsync());
            documentoError.RootElement.GetProperty("codigo").GetString()
                .Should().Be("PERMISO_DENEGADO", ruta);
        }
    }

    [Fact]
    public async Task Debe_RechazarUnTokenInvalidoCon401()
    {
        using var cliente = _fixture.CrearCliente();
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "/api/v1/convocatorias");
        solicitud.Headers.Add(AutenticacionPruebaHandler.CabeceraTokenInvalido, "true");

        var respuesta = await cliente.SendAsync(solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Debe_ConsultarListadosVaciosYDevolver404ParaUnaConvocatoriaInexistente()
    {
        using var cliente = _fixture.CrearCliente();

        using var consultaPublica = CrearSolicitud(
            HttpMethod.Get,
            "/api/v1/convocatorias",
            permiso: "convocatorias:leer");
        var respuestaPublica = await cliente.SendAsync(consultaPublica);
        respuestaPublica.StatusCode.Should().Be(HttpStatusCode.OK);

        using var consultaInterna = CrearSolicitud(
            HttpMethod.Get,
            "/internal/v1/convocatorias/abiertas",
            permiso: "convocatorias:leer");
        var respuestaInterna = await cliente.SendAsync(consultaInterna);
        respuestaInterna.StatusCode.Should().Be(HttpStatusCode.OK);

        using var detalle = CrearSolicitud(
            HttpMethod.Get,
            $"/api/v1/convocatorias/{Guid.NewGuid():D}",
            permiso: "convocatorias:leer");
        var respuestaDetalle = await cliente.SendAsync(detalle);
        respuestaDetalle.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Debe_CrearEditarConfigurarYPublicarUnaConvocatoriaPorHttp()
    {
        using var cliente = _fixture.CrearCliente();
        var (programaId, universidadId, carreraId) = await CrearProgramaHabilitadoAsync();
        var periodoId = Guid.NewGuid();
        _fixture.ConfigurarPeriodoVigente(periodoId);

        var fechaApertura = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var fechaCierre = fechaApertura.AddDays(10);
        var fechaSubsanacion = fechaCierre.AddDays(2);
        var fechaInicioEvaluacion = fechaSubsanacion;
        var fechaCierreEvaluacion = fechaSubsanacion.AddDays(10);
        var criterios = new CriteriosElegibilidadCicloDto(
            15.00m,
            17,
            35,
            1200.00m,
            [universidadId],
            [carreraId]);
        var solicitudCrear = new CrearConvocatoriaRequest(
            programaId,
            periodoId.ToString("D"),
            $"API-{Guid.NewGuid():N}"[..20],
            "Convocatoria API",
            fechaApertura,
            fechaCierre,
            fechaSubsanacion,
            fechaInicioEvaluacion,
            fechaCierreEvaluacion,
            true,
            3,
            criterios,
            _usuarioId);

        using var crear = CrearSolicitud(
            HttpMethod.Post,
            "/api/v1/convocatorias",
            "convocatorias:crear",
            solicitudCrear);
        var respuestaCrear = await cliente.SendAsync(crear);
        respuestaCrear.StatusCode.Should().Be(HttpStatusCode.Created);
        var detalleCreado = await respuestaCrear.Content.ReadFromJsonAsync<ConvocatoriaDetalleDto>();
        detalleCreado.Should().NotBeNull();
        detalleCreado!.Estado.Should().Be("BORRADOR");

        using var crearDuplicada = CrearSolicitud(
            HttpMethod.Post,
            "/api/v1/convocatorias",
            "convocatorias:crear",
            solicitudCrear);
        var respuestaDuplicada = await cliente.SendAsync(crearDuplicada);
        respuestaDuplicada.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var jsonDuplicado = await JsonDocument.ParseAsync(
            await respuestaDuplicada.Content.ReadAsStreamAsync()))
        {
            jsonDuplicado.RootElement.GetProperty("codigo").GetString()
                .Should().Be("CODIGO_CONVOCATORIA_DUPLICADO");
        }

        using var detalle = CrearSolicitud(
            HttpMethod.Get,
            $"/api/v1/convocatorias/{detalleCreado.Id:D}",
            permiso: "convocatorias:leer");
        (await cliente.SendAsync(detalle)).StatusCode.Should().Be(HttpStatusCode.OK);

        var solicitudEditar = new EditarConvocatoriaRequest(
            periodoId.ToString("D"),
            "Convocatoria API editada",
            fechaApertura,
            fechaCierre,
            fechaSubsanacion,
            fechaInicioEvaluacion,
            fechaCierreEvaluacion,
            true,
            3,
            criterios,
            _usuarioId);
        using var editar = CrearSolicitud(
            HttpMethod.Put,
            $"/api/v1/convocatorias/{detalleCreado.Id:D}",
            "convocatorias:editar",
            solicitudEditar);
        (await cliente.SendAsync(editar)).StatusCode.Should().Be(HttpStatusCode.OK);

        var solicitudDocumento = new AgregarDocumentoRequeridoRequest(
            "IDENTIDAD",
            "Documento de identidad",
            null,
            true,
            ["application/pdf"],
            5,
            _usuarioId);
        using var agregarDocumento = CrearSolicitud(
            HttpMethod.Post,
            $"/api/v1/convocatorias/{detalleCreado.Id:D}/documentos-requeridos",
            "convocatorias:editar",
            solicitudDocumento);
        var respuestaAgregar = await cliente.SendAsync(agregarDocumento);
        respuestaAgregar.StatusCode.Should().Be(HttpStatusCode.Created);
        var documento = await respuestaAgregar.Content.ReadFromJsonAsync<DocumentoRequeridoDto>();
        documento.Should().NotBeNull();

        using var eliminarDocumento = CrearSolicitud(
            HttpMethod.Delete,
            $"/api/v1/convocatorias/{detalleCreado.Id:D}/documentos-requeridos/{documento!.Id:D}",
            permiso: "convocatorias:editar");
        (await cliente.SendAsync(eliminarDocumento)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var agregarDocumentoParaPublicar = CrearSolicitud(
            HttpMethod.Post,
            $"/api/v1/convocatorias/{detalleCreado.Id:D}/documentos-requeridos",
            "convocatorias:editar",
            solicitudDocumento);
        (await cliente.SendAsync(agregarDocumentoParaPublicar)).StatusCode.Should().Be(HttpStatusCode.Created);

        using var publicar = CrearSolicitud(
            HttpMethod.Post,
            $"/api/v1/convocatorias/{detalleCreado.Id:D}/publicar",
            "convocatorias:publicar",
            new PublicarConvocatoriaRequest(_usuarioId));
        var respuestaPublicar = await cliente.SendAsync(publicar);
        respuestaPublicar.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuestaPublicar.Content.ReadFromJsonAsync<ConvocatoriaDetalleDto>())
            ?.Estado.Should().Be("PUBLICADA");
    }

    [Fact]
    public async Task Debe_PublicarLasRutasYCodigosDeErrorEnOpenApi()
    {
        using var cliente = _fixture.CrearCliente();

        using var documento = await cliente.GetAsync("/openapi/v1.json");
        documento.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await JsonDocument.ParseAsync(await documento.Content.ReadAsStreamAsync());
        var rutas = json.RootElement.GetProperty("paths");
        rutas.TryGetProperty("/api/v1/convocatorias", out var rutaCrear).Should().BeTrue();
        rutaCrear.GetProperty("post").GetProperty("responses").TryGetProperty("401", out _).Should().BeTrue();
        rutaCrear.GetProperty("post").GetProperty("responses").TryGetProperty("403", out _).Should().BeTrue();
        rutas.TryGetProperty("/internal/v1/convocatorias/abiertas", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_IniciarHangfireYRegistrarElCierreRecurrenteEnPostgreSql()
    {
        await using var conexion = new NpgsqlConnection(_fixture.ConnectionString);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT COUNT(DISTINCT tablename) FROM pg_tables WHERE tablename IN ('job', 'hash')",
            conexion);
        var tablas = (long)(await comando.ExecuteScalarAsync())!;
        tablas.Should().Be(2);

        await using var consultaJob = new NpgsqlCommand(
            """
            SELECT value
            FROM hangfire.hash
            WHERE key = @clave AND field = 'Cron'
            """,
            conexion);
        consultaJob.Parameters.AddWithValue(
            "clave",
            $"recurring-job:{ConfiguracionHangfire.IdJobCierre}");
        (await consultaJob.ExecuteScalarAsync()).Should().Be("*/1 * * * *");
    }

    private async Task<(Guid ProgramaId, Guid UniversidadId, Guid CarreraId)> CrearProgramaHabilitadoAsync()
    {
        var codigo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var universidad = UniversidadAliada.CrearConConvenioInicial(
            $"API-{codigo}",
            $"Universidad API {codigo}",
            "Caracas",
            new ContactoEnlace("Ana API", $"ana-{codigo}@example.org", "+58 212 5550000"),
            null,
            null,
            $"CONV-API-{codigo}",
            "Convenio de prueba API",
            new DateOnly(2020, 1, 1),
            new DateOnly(2035, 12, 31),
            $"/universidades/api-{codigo}/convenio.pdf",
            true,
            _usuarioId);
        var carrera = universidad.AgregarCarreraElegible(
            $"ING-{codigo}",
            "Ingeniería de prueba",
            10,
            _usuarioId);
        var donante = Donante.Crear(
            TipoDonante.Empresa,
            $"Fundación API {codigo}",
            $"J-{codigo}-1",
            $"finanzas-{codigo}@example.org",
            "+58 212 5550001",
            "Caracas",
            new ContactoDonante(
                "Ana API",
                "Directora",
                $"contacto-{codigo}@example.org",
                "+58 212 5550002"),
            _usuarioId);
        var programa = ProgramaBeca.Crear(
            $"API-{codigo}",
            $"Programa API {codigo}",
            "Programa de integración de la API.",
            new PoliticaCobertura(ModalidadCobertura.Total, 100.00m, 0.00m, "USD"),
            10,
            8,
            "2026-II",
            new DateOnly(2020, 1, 1),
            new DateOnly(2035, 12, 31),
            false,
            true,
            null,
            _usuarioId);
        programa.AsignarFondoDonante(donante.Id, "2026-II", 20000.00m, _usuarioId);
        programa.ConfigurarElegibilidadYPermanencia(
            new CriteriosElegibilidadPrograma(
                15.00m,
                17,
                35,
                1200.00m,
                [universidad.Id],
                [carrera.Id]),
            new CondicionesPermanencia(
                14.00m,
                4,
                1,
                10,
                FrecuenciaReporteAcademico.Semestral,
                ConsecuenciaIncumplimiento.Suspension,
                0.50m,
                2,
                true),
            todasLasUniversidadesTienenConvenioVigente: true,
            todasLasCarrerasSonElegibles: true,
            _usuarioId);

        await using var contexto = CrearContexto();
        await contexto.Donantes.AddAsync(donante);
        await contexto.UniversidadesAliadas.AddAsync(universidad);
        await contexto.ProgramasBeca.AddAsync(programa);
        await contexto.SaveChangesAsync();

        return (programa.Id, universidad.Id, carrera.Id);
    }

    private ProgramasDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ProgramasDbContext>()
            .UseNpgsql(
                _fixture.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    ProgramasDbContext.Esquema))
            .Options;

        return new ProgramasDbContext(opciones);
    }

    private static HttpRequestMessage CrearSolicitud(
        HttpMethod metodo,
        string ruta,
        string? permiso = null,
        object? cuerpo = null,
        bool incluirAutenticacion = true)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        if (incluirAutenticacion)
        {
            solicitud.Headers.Add(AutenticacionPruebaHandler.CabeceraAutenticado, "true");
        }

        if (permiso is not null)
        {
            solicitud.Headers.Add(AutenticacionPruebaHandler.CabeceraPermiso, permiso);
        }

        if (cuerpo is not null)
        {
            solicitud.Content = JsonContent.Create(cuerpo);
        }

        return solicitud;
    }
}

public sealed class ConvocatoriasApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly Dictionary<string, string?> _environmentValues = new(StringComparer.Ordinal);
    private WebApplicationFactory<global::Program>? _factory;

    public string ConnectionString => _postgres.GetConnectionString();

    public WireMockServer Periodos { get; private set; } = null!;

    public WebApplicationFactory<global::Program> Factory
        => _factory ?? throw new InvalidOperationException("La API de pruebas aún no está inicializada.");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Periodos = WireMockServer.Start();

        var urlPeriodos = Periodos.Url
            ?? throw new InvalidOperationException("WireMock no proporcionó su URL de escucha.");
        EstablecerVariableEntorno("ConnectionStrings__Programas", ConnectionString);
        EstablecerVariableEntorno("ServiciosExternos__PeriodoAcademico__UrlBase", urlPeriodos);
        EstablecerVariableEntorno("Hangfire__Habilitado", "true");
        EstablecerVariableEntorno("Hangfire__IntervaloCierre", "*/1 * * * *");

        try
        {
            _factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuracion) =>
                    configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Programas"] = ConnectionString,
                        ["Keycloak:Authority"] = string.Empty,
                        ["Keycloak:Audience"] = string.Empty,
                        ["Keycloak:RequireHttpsMetadata"] = "false",
                        ["ServiciosExternos:PeriodoAcademico:UrlBase"] = urlPeriodos,
                        ["Hangfire:Habilitado"] = "true",
                        ["Hangfire:IntervaloCierre"] = "*/1 * * * *"
                    }));

                builder.ConfigureTestServices(servicios =>
                    servicios
                        .AddAuthentication(opciones =>
                        {
                            opciones.DefaultAuthenticateScheme = AutenticacionPruebaHandler.Esquema;
                            opciones.DefaultChallengeScheme = AutenticacionPruebaHandler.Esquema;
                            opciones.DefaultForbidScheme = AutenticacionPruebaHandler.Esquema;
                        })
                        .AddScheme<AuthenticationSchemeOptions, AutenticacionPruebaHandler>(
                            AutenticacionPruebaHandler.Esquema,
                            _ => { }));
            });

            using var cliente = _factory.CreateClient();
        }
        finally
        {
            RestaurarVariablesEntorno();
        }
    }

    public HttpClient CrearCliente()
        => Factory.CreateClient();

    public void ConfigurarPeriodoVigente(Guid periodoId)
    {
        Periodos.Given(Request.Create()
                .WithPath($"/internal/v1/periodos-academicos/{periodoId:D}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = periodoId,
                    nombre = "2026-II",
                    estado = "VIGENTE"
                })));
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        Periodos?.Dispose();
        RestaurarVariablesEntorno();
        await _postgres.DisposeAsync();
    }

    private void EstablecerVariableEntorno(string nombre, string valor)
    {
        _environmentValues[nombre] = Environment.GetEnvironmentVariable(nombre);
        Environment.SetEnvironmentVariable(nombre, valor);
    }

    private void RestaurarVariablesEntorno()
    {
        foreach (var (nombre, valor) in _environmentValues)
        {
            Environment.SetEnvironmentVariable(nombre, valor);
        }

        _environmentValues.Clear();
    }
}

public sealed class AutenticacionPruebaHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "AutenticacionPrueba";
    public const string CabeceraAutenticado = "X-Test-Authenticated";
    public const string CabeceraPermiso = "X-Test-Permission";
    public const string CabeceraTokenInvalido = "X-Test-Invalid";

    public AutenticacionPruebaHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : base(opciones, logger, codificador)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(CabeceraTokenInvalido))
        {
            return Task.FromResult(AuthenticateResult.Fail("Token de prueba inválido."));
        }

        if (!Request.Headers.TryGetValue(CabeceraAutenticado, out var autenticado)
            || !string.Equals(autenticado.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("sub", "01966000-0000-7000-8000-000000000701")
        };
        claims.AddRange(Request.Headers[CabeceraPermiso]
            .Where(valor => !string.IsNullOrWhiteSpace(valor))
            .Select(valor => new Claim("permissions", valor!)));

        var identidad = new ClaimsIdentity(claims, Esquema);
        var principal = new ClaimsPrincipal(identidad);
        var ticket = new AuthenticationTicket(principal, Esquema);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
