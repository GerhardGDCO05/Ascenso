using System.Security.Claims;
using System.Text.Encodings.Web;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.IntegrationTests.EndToEnd;

/// <summary>
/// Levanta la API real con PostgreSQL y WireMock. Solo se sustituyen las
/// fronteras que no forman parte del flujo bajo prueba: autenticación,
/// generación de PDF, almacenamiento y Outbox (capturado en memoria).
/// Hangfire se mantiene activo con sondeo rápido para validar la expiración.
/// </summary>
public sealed class FabricaApiExtremoAExtremo : WebApplicationFactory<Program>
{
    private const string EsquemaPrueba = "TestJwt";
    private readonly ExtremoAExtremoFixture _fixture;
    private readonly OutboxExtremoAExtremo _outbox = new();

    public FabricaApiExtremoAExtremo(ExtremoAExtremoFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    internal OutboxExtremoAExtremo Outbox => _outbox;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Becas", _fixture.CadenaConexion);
        builder.UseSetting("Mensajeria:Habilitada", "false");
        builder.UseSetting("Hangfire:Dashboard:Habilitado", "false");
        builder.UseSetting("Observabilidad:Telemetria:Habilitada", "false");
        builder.UseSetting("Observabilidad:MetricasPendientes:Habilitada", "false");
        builder.UseSetting("Clientes:Postulaciones:BaseUrl", _fixture.WireMock.Url!);
        builder.UseSetting("Clientes:Postulaciones:UsaServiceToken", "false");
        builder.UseSetting("Clientes:Programas:BaseUrl", _fixture.WireMock.Url!);
        builder.UseSetting("Clientes:Programas:UsaServiceToken", "false");
        builder.UseSetting("Clientes:Plataforma:BaseUrl", _fixture.WireMock.Url!);
        builder.UseSetting("Clientes:Plataforma:UsaServiceToken", "false");
        builder.UseSetting("Storage:Provider", "Local");
        builder.UseSetting(
            "Storage:Local:RutaBase",
            Path.Combine(Path.GetTempPath(), "ascenso-becas-e2e"));

        builder.ConfigureTestServices(servicios =>
        {
            EliminarServidoresHangfire(servicios);
            EliminarPublicadorSalud(servicios);

            servicios
                .AddAuthentication(opciones =>
                {
                    opciones.DefaultAuthenticateScheme = EsquemaPrueba;
                    opciones.DefaultChallengeScheme = EsquemaPrueba;
                    opciones.DefaultScheme = EsquemaPrueba;
                })
                .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionE2E>(
                    EsquemaPrueba,
                    _ => { });

            servicios.RemoveAll<IOutboxBeca>();
            servicios.AddSingleton<IOutboxBeca>(_outbox);

            servicios.RemoveAll<ICartaOfertaPdfGenerator>();
            servicios.AddSingleton<ICartaOfertaPdfGenerator, CartaOfertaExtremoAExtremo>();

            servicios.RemoveAll<IPlantillaCompromisoPdfGenerator>();
            servicios.AddSingleton<
                IPlantillaCompromisoPdfGenerator,
                PlantillaCompromisoExtremoAExtremo>();

            servicios.RemoveAll<IAlmacenamientoArchivos>();
            servicios.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoExtremoAExtremo>();

            servicios.RemoveAll<IPlanDesembolsoPrograma>();
            servicios.AddSingleton<IPlanDesembolsoPrograma, PlanDesembolsoExtremoAExtremo>();

            servicios.AddHangfireServer(opciones =>
            {
                opciones.Queues =
                    [RegistroHangfireColaCritica, RegistroHangfireColaPorDefecto];
                opciones.SchedulePollingInterval = TimeSpan.FromMilliseconds(500);
                opciones.WorkerCount = 1;
                opciones.ServerName = $"e2e-{Guid.NewGuid():N}";
            });
        });
    }

    private const string RegistroHangfireColaCritica = "critical";
    private const string RegistroHangfireColaPorDefecto = "default";

    private static void EliminarServidoresHangfire(IServiceCollection servicios)
    {
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

    private static void EliminarPublicadorSalud(IServiceCollection servicios)
    {
        var descriptores = servicios
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && (EsDeSalud(descriptor.ImplementationType)
                    || EsDeSalud(
                        descriptor.ImplementationFactory?.Method.DeclaringType)))
            .ToList();

        foreach (var descriptor in descriptores)
        {
            servicios.Remove(descriptor);
        }

        static bool EsDeSalud(Type? tipo)
            => tipo?.FullName?.Contains(
                "HealthCheck",
                StringComparison.OrdinalIgnoreCase) == true;
    }

    private sealed class ManejadorAutenticacionE2E
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ManejadorAutenticacionE2E(
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

internal sealed class PlanDesembolsoExtremoAExtremo : IPlanDesembolsoPrograma
{
    public Task<PlanDesembolsoPrograma?> ObtenerAsync(Guid programaBecaId,
        Guid periodoAcademicoId, CancellationToken cancellationToken = default)
    {
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow);
        return Task.FromResult<PlanDesembolsoPrograma?>(new(
            programaBecaId, periodoAcademicoId,
            FrecuenciaDesembolso.Mensual, DestinatarioPago.Becario,
            fecha, fecha, [new CuotaProgramada(1, fecha, 100m)]));
    }
}
