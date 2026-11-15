using System.Security.Claims;
using System.Text.Json;
using Ascenso.Becas.Api.Seguridad;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.IntegrationTests.Seguridad;

public sealed class Autorizacion2FATests
{
    private const string AcrValido = "urn:ascenso:loa:2fa";

    [Fact]
    public async Task Debe_Autorizar_Cuando_ElAcrCoincideConElValorConfigurado()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(acr: AcrValido));

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Autorizar_Cuando_ElAmrContieneOtp()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(amr: ["pwd", "otp"]));

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Autorizar_Cuando_ElAmrContieneTotpEnUnArregloJson()
    {
        using var proveedor = CrearProveedor();
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("roles", "COORDINADOR_PROGRAMA"),
            new Claim("amr", "[\"pwd\",\"totp\"]")
        ], "Bearer"));

        var resultado = await AutorizarAsync(proveedor, usuario);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_ElRolAdministrativoNoTieneSegundoFactor()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(proveedor, CrearUsuario());

        resultado.Succeeded.Should().BeFalse();
        resultado.Failure.Should().NotBeNull();
        resultado.Failure!.FailedRequirements.Should()
            .ContainSingle(requisito => requisito is Requiere2FARequirement);
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_AcrYAmrNoAcreditanSegundoFactor()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(acr: "urn:otro:loa", amr: ["pwd"]));

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_UsarElAcrConfigurado_Cuando_SeSobrescribeLaOpcion()
    {
        using var proveedor = CrearProveedor(opciones =>
            opciones.AcrSegundoFactor = "urn:personalizado:2fa");

        var conAcrPersonalizado = await AutorizarAsync(
            proveedor,
            CrearUsuario(acr: "urn:personalizado:2fa"));
        var conAcrPorDefecto = await AutorizarAsync(
            proveedor,
            CrearUsuario(acr: AcrValido));

        conAcrPersonalizado.Succeeded.Should().BeTrue();
        conAcrPorDefecto.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_OmitirElSegundoFactor_Cuando_ElRolNoEsAdministrativo()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(roles: ["POSTULANTE"]));

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Responder401ConCodigo_Cuando_FallaElSegundoFactor()
    {
        using var proveedor = CrearProveedor();
        var autorizacion = await AutorizarAsync(proveedor, CrearUsuario());
        autorizacion.Succeeded.Should().BeFalse();

        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/becas/ofertas";
        contexto.Response.Body = new MemoryStream();

        var resultado = PolicyAuthorizationResult.Forbid(autorizacion.Failure);
        var manejador = new AutorizacionResultadoHandler();

        await manejador.HandleAsync(
            _ => Task.CompletedTask,
            contexto,
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
            resultado);

        contexto.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        contexto.Response.ContentType.Should().Be("application/problem+json");

        contexto.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        documento.RootElement.GetProperty("code").GetString()
            .Should().Be("SEGUNDO_FACTOR_REQUERIDO");
    }

    private static ServiceProvider CrearProveedor(
        Action<AutorizacionOptions>? configurar = null)
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarAutorizacion(new ConfigurationBuilder().Build());

        if (configurar is not null)
        {
            servicios.Configure(configurar);
        }

        return servicios.BuildServiceProvider();
    }

    private static Task<AuthorizationResult> AutorizarAsync(
        ServiceProvider proveedor,
        ClaimsPrincipal usuario)
        => proveedor
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(usuario, resource: null, PoliticasSeguridad.Requiere2FA);

    private static ClaimsPrincipal CrearUsuario(
        string? acr = null,
        string[]? amr = null,
        string[]? roles = null)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString())
        };

        foreach (var rol in roles ?? ["COORDINADOR_PROGRAMA"])
        {
            claims.Add(new Claim("roles", rol));
        }

        if (acr is not null)
        {
            claims.Add(new Claim("acr", acr));
        }

        foreach (var valor in amr ?? [])
        {
            claims.Add(new Claim("amr", valor));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}
