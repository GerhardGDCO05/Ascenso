using Ascenso.Programas.Api.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ascenso.Programas.IntegrationTests.Seguridad;

public sealed class ConfiguracionSeguridadTests
{
    [Fact]
    public async Task Debe_ResolverPoliticasExistentesYNuevasPoliticasDePermisos()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddAuthorization(opciones =>
            opciones.AddPolicy("politica-por-rol", politica => politica.RequireRole("operador")));

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = string.Empty,
                ["Keycloak:Audience"] = string.Empty
            })
            .Build();

        servicios.AgregarSeguridadProgramas(configuracion);

        using var proveedorServicios = servicios.BuildServiceProvider();
        var proveedorPoliticas = proveedorServicios.GetRequiredService<IAuthorizationPolicyProvider>();
        var esquemas = await proveedorServicios
            .GetRequiredService<IAuthenticationSchemeProvider>()
            .GetAllSchemesAsync();
        Assert.Contains(
            esquemas,
            esquema => esquema.Name == AutenticacionNoConfiguradaHandler.Esquema);

        var politicaExistente = await proveedorPoliticas.GetPolicyAsync("politica-por-rol");
        Assert.NotNull(politicaExistente);
        Assert.Contains(politicaExistente.Requirements, requisito => requisito is RolesAuthorizationRequirement);

        var politicaConvocatorias = await proveedorPoliticas.GetPolicyAsync(PermisosConvocatoria.Publicar);
        Assert.NotNull(politicaConvocatorias);
        Assert.Contains(politicaConvocatorias.Requirements, requisito => requisito is DenyAnonymousAuthorizationRequirement);
        Assert.Contains(
            politicaConvocatorias.Requirements,
            requisito => requisito is RequisitoPermiso permiso
                && permiso.Permiso == PermisosConvocatoria.Publicar);
    }

    [Fact]
    public void Debe_ConfigurarJwtBearerConLosClaimsDeKeycloak()
    {
        const string authority = "https://identidad.example.org/realms/ascenso";
        const string audience = "ascenso-api";
        var servicios = new ServiceCollection();
        servicios.AddLogging();

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = authority,
                ["Keycloak:Audience"] = audience,
                ["Keycloak:RequireHttpsMetadata"] = "true",
                ["Keycloak:RoleClaimType"] = "roles"
            })
            .Build();

        servicios.AgregarSeguridadProgramas(configuracion);

        using var proveedorServicios = servicios.BuildServiceProvider();
        var opciones = proveedorServicios
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(authority, opciones.Authority);
        Assert.Equal(audience, opciones.Audience);
        Assert.True(opciones.RequireHttpsMetadata);
        Assert.False(opciones.MapInboundClaims);
        Assert.True(opciones.TokenValidationParameters.ValidateIssuer);
        Assert.Equal(authority, opciones.TokenValidationParameters.ValidIssuer);
        Assert.Equal("roles", opciones.TokenValidationParameters.RoleClaimType);
    }
}
