using System.Security.Claims;
using Ascenso.Postulaciones.Api.Seguridad;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Ascenso.Postulaciones.IntegrationTests.Seguridad;

public sealed class RolesUsuarioActualJwtTests
{
    [Fact]
    public void Debe_LanzarExcepcion_Cuando_HttpContextAccessorEsNuloONombreRolEsInvalido()
    {
        var ctorNulo = () => new RolesUsuarioActualJwt(null!);
        ctorNulo.Should().Throw<ArgumentNullException>();

        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var rolesUsuario = new RolesUsuarioActualJwt(accessor);

        var rolNulo = () => rolesUsuario.TieneRol(null!);
        var rolVacio = () => rolesUsuario.TieneRol("   ");

        rolNulo.Should().Throw<ArgumentException>();
        rolVacio.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Debe_RetornarTrue_Cuando_UsuarioAutenticadoPoseeElRolEnClaimRolesOIsInRole()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", Guid.NewGuid().ToString()),
                    new Claim("roles", "COORDINADOR_PROGRAMA"),
                    new Claim(ClaimTypes.Role, "MIEMBRO_COMITE")
                ],
                authenticationType: "Bearer",
                nameType: "sub",
                roleType: "roles"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var rolesUsuario = new RolesUsuarioActualJwt(accessor);

        rolesUsuario.TieneRol("COORDINADOR_PROGRAMA").Should().BeTrue();
        rolesUsuario.TieneRol("MIEMBRO_COMITE").Should().BeTrue();
        rolesUsuario.TieneRol("DIRECCION").Should().BeFalse();
    }

    [Fact]
    public void Debe_RetornarFalse_Cuando_NoHayContextoOUsuarioNoEstaAutenticado()
    {
        var sinContexto = new RolesUsuarioActualJwt(new HttpContextAccessor());
        sinContexto.TieneRol("COORDINADOR_PROGRAMA").Should().BeFalse();

        var noAutenticado = new RolesUsuarioActualJwt(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("roles", "COORDINADOR_PROGRAMA")]))
            }
        });

        noAutenticado.TieneRol("COORDINADOR_PROGRAMA").Should().BeFalse();
    }
}
