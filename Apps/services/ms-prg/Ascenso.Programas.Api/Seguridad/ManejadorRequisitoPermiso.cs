using Microsoft.AspNetCore.Authorization;

namespace Ascenso.Programas.Api.Seguridad;

/// <summary>
/// Comprueba que el principal autenticado tenga el permiso exigido. Cada
/// elemento del arreglo JWT <c>permissions</c> se representa como un claim.
/// </summary>
public sealed class ManejadorRequisitoPermiso : AuthorizationHandler<RequisitoPermiso>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequisitoPermiso requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var tienePermiso = context.User
            .FindAll(PermisosConvocatoria.ClaimPermisos)
            .Any(claim => string.Equals(
                claim.Value,
                requirement.Permiso,
                StringComparison.Ordinal));

        if (tienePermiso)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
