using System.Security.Claims;
using Ascenso.Postulaciones.Api.Seguridad;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Ascenso.Postulaciones.IntegrationTests.Seguridad;

public sealed class UsuarioActualJwtTests
{
    [Fact]
    public void Debe_ResolverUsuarioId_Cuando_TokenTieneClaimSubValido()
    {
        var usuarioId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", usuarioId.ToString())],
                authenticationType: "Bearer"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var usuarioActual = new UsuarioActualJwt(accessor);

        usuarioActual.EstaAutenticado.Should().BeTrue();
        usuarioActual.UsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public void Debe_RetornarNoAutenticado_Cuando_TokenNoTieneClaimSub()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("roles", "POSTULANTE")],
                authenticationType: "Bearer"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var usuarioActual = new UsuarioActualJwt(accessor);

        usuarioActual.EstaAutenticado.Should().BeFalse();
        usuarioActual.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Debe_RetornarNoAutenticado_Cuando_NoHayAutenticacion()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var usuarioActual = new UsuarioActualJwt(accessor);

        usuarioActual.EstaAutenticado.Should().BeFalse();
        usuarioActual.UsuarioId.Should().Be(Guid.Empty);
    }
}
