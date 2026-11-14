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

public sealed class AutorizacionCuentaServicioTests
{
    private const string ClientePermitido = "ms-bec-internal";
    private const string ScopeInterno = "becas.internal.read";

    [Fact]
    public async Task Debe_Autorizar_AlClientePermitidoConElScopeInterno()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(ClientePermitido, ScopeInterno));

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Autorizar_Cuando_ElScopeLlegaSeparadoPorEspacios()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(ClientePermitido, "openid perfil becas.internal.read"));

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Autorizar_Cuando_ElScopeLlegaComoArregloJson()
    {
        using var proveedor = CrearProveedor();
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("client_id", ClientePermitido),
            new Claim("scope", "[\"openid\",\"becas.internal.read\"]")
        ], "Bearer"));

        var resultado = await AutorizarAsync(proveedor, usuario);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_ElClienteNoEstaPermitido()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario("cliente-externo", ScopeInterno));

        resultado.Succeeded.Should().BeFalse();
        resultado.Failure!.FailedRequirements.Should()
            .ContainSingle(requisito => requisito is CuentaServicioRequirement);
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_FaltaElClaimClientId()
    {
        using var proveedor = CrearProveedor();
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("scope", ScopeInterno)
        ], "Bearer"));

        var resultado = await AutorizarAsync(proveedor, usuario);

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_ElScopeEsInsuficiente()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(ClientePermitido, "openid perfil"));

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_ElScopeSoloCoincideParcialmente()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(ClientePermitido, "becas.internal.read.extra"));

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElScopeEsInsuficiente()
    {
        using var proveedor = CrearProveedor();
        var autorizacion = await AutorizarAsync(
            proveedor,
            CrearUsuario(ClientePermitido, "openid perfil"));

        var respuesta = await EjecutarManejadorAsync(autorizacion);

        respuesta.Estado.Should().Be(StatusCodes.Status403Forbidden);
        respuesta.Codigo.Should().Be("PERMISO_DENEGADO");
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_LaLlamadaInternaNoTieneToken()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/internal/v1/becas/activas";
        contexto.Response.Body = new MemoryStream();

        await new AutorizacionResultadoHandler().HandleAsync(
            _ => Task.CompletedTask,
            contexto,
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
            PolicyAuthorizationResult.Challenge());

        contexto.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        contexto.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        documento.RootElement.GetProperty("code").GetString()
            .Should().Be("NO_AUTENTICADO");
    }

    private static ServiceProvider CrearProveedor()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarAutorizacion(new ConfigurationBuilder().Build());

        return servicios.BuildServiceProvider();
    }

    private static Task<AuthorizationResult> AutorizarAsync(
        ServiceProvider proveedor,
        ClaimsPrincipal usuario)
        => proveedor
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(usuario, resource: null, PoliticasSeguridad.ServiceAccountInterno);

    private static ClaimsPrincipal CrearUsuario(string cliente, params string[] scopes)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("client_id", cliente)
        };

        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static async Task<(int Estado, string Codigo)> EjecutarManejadorAsync(
        AuthorizationResult autorizacion)
    {
        autorizacion.Succeeded.Should().BeFalse();

        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/internal/v1/becas/activas";
        contexto.Response.Body = new MemoryStream();

        await new AutorizacionResultadoHandler().HandleAsync(
            _ => Task.CompletedTask,
            contexto,
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
            PolicyAuthorizationResult.Forbid(autorizacion.Failure));

        contexto.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);

        return (
            contexto.Response.StatusCode,
            documento.RootElement.GetProperty("code").GetString()!);
    }
}
