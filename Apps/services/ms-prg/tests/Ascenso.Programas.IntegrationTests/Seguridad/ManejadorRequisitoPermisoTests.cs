using System.Security.Claims;
using Ascenso.Programas.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;

namespace Ascenso.Programas.IntegrationTests.Seguridad;

public sealed class ManejadorRequisitoPermisoTests
{
    [Fact]
    public async Task Debe_AutorizarCuandoPermisoEsElementoDelArreglo()
    {
        var contexto = CrearContexto("convocatorias:publicar");

        await new ManejadorRequisitoPermiso().HandleAsync(contexto);

        Assert.True(contexto.HasSucceeded);
    }

    [Theory]
    [InlineData("[\"convocatorias:publicar\"]")]
    [InlineData("convocatorias:leer,convocatorias:publicar")]
    public async Task Debe_RechazarCuandoPermisosNoSonElementosDelArreglo(string valor)
    {
        var contexto = CrearContexto(valor);

        await new ManejadorRequisitoPermiso().HandleAsync(contexto);

        Assert.False(contexto.HasSucceeded);
    }

    private static AuthorizationHandlerContext CrearContexto(string valorPermiso)
    {
        var identidad = new ClaimsIdentity(
            [new Claim(PermisosConvocatoria.ClaimPermisos, valorPermiso)],
            "prueba");

        return new AuthorizationHandlerContext(
            [new RequisitoPermiso(PermisosConvocatoria.Publicar)],
            new ClaimsPrincipal(identidad),
            resource: null);
    }
}
