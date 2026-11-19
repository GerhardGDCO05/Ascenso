using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.IntegrationTests.Persistencia;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class HangfireDashboardTests
{
    private const string EncabezadoRoles = "X-Test-Roles";
    private const string EncabezadoAcr = "X-Test-Acr";

    private readonly PostgreSqlFixture _fixture;

    public HangfireDashboardTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_NoHayAutenticacion()
    {
        using var fabrica = new DashboardApiFactory(_fixture.CadenaConexion);
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/hangfire");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElUsuarioNoEsAdministrativo()
    {
        using var fabrica = new DashboardApiFactory(_fixture.CadenaConexion);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(EncabezadoRoles, "POSTULANTE");

        var respuesta = await cliente.GetAsync("/hangfire");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Debe_Responder200_Cuando_ElUsuarioEsAdministrativoCon2FA()
    {
        using var fabrica = new DashboardApiFactory(_fixture.CadenaConexion);
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(EncabezadoRoles, "DIRECCION");
        cliente.DefaultRequestHeaders.Add(EncabezadoAcr, "urn:ascenso:loa:2fa");

        // El host debe usar la base del contenedor y no la de appsettings.
        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<BecasDbContext>();
        contexto.Database.GetConnectionString().Should().Be(_fixture.CadenaConexion);

        var respuesta = await cliente.GetAsync("/hangfire");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class DashboardApiFactory : WebApplicationFactory<Program>
    {
        private const string EsquemaPrueba = "TestJwt";

        private readonly string _cadenaConexion;

        public DashboardApiFactory(string cadenaConexion)
        {
            _cadenaConexion = cadenaConexion;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseEnvironment("Development");

            // Program.cs lee la configuración antes de que se apliquen las
            // fuentes de ConfigureAppConfiguration; UseSetting sí está
            // disponible en ese momento.
            builder.UseSetting("ConnectionStrings:Becas", _cadenaConexion);
            builder.UseSetting("Mensajeria:Habilitada", "false");

            builder.ConfigureTestServices(servicios =>
            {
                // Los adaptadores de PDF y almacenamiento se implementan en
                // tareas posteriores; aquí solo se satisface la validación.
                servicios.RemoveAll<ICartaOfertaPdfGenerator>();
                servicios.AddSingleton(Substitute.For<ICartaOfertaPdfGenerator>());
                servicios.RemoveAll<IAlmacenamientoArchivos>();
                servicios.AddSingleton(Substitute.For<IAlmacenamientoArchivos>());

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
            });
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
                if (!Request.Headers.ContainsKey("X-Test-Roles"))
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

                var acr = Request.Headers["X-Test-Acr"].ToString();

                if (!string.IsNullOrWhiteSpace(acr))
                {
                    claims.Add(new Claim("acr", acr));
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
}
