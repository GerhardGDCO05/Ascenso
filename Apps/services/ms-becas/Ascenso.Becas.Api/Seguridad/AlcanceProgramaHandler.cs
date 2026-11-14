using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Autoriza a un coordinador cuando el programa del recurso está dentro de su
/// claim <c>programa_ids</c>. <c>DIRECCION</c> puede operar globalmente.
/// </summary>
public sealed class AlcanceProgramaHandler : AuthorizationHandler<AlcanceProgramaRequirement>
{
    private readonly AutorizacionOptions _opciones;

    public AlcanceProgramaHandler(IOptions<AutorizacionOptions> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _opciones = opciones.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AlcanceProgramaRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        // Sin el programa del recurso no se puede evaluar el alcance: se falla
        // cerrado en lugar de autorizar de más.
        if (context.Resource is not IRecursoAutorizable recurso
            || recurso.ProgramaId is not { } programaId
            || programaId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        if (TieneRolDireccion(context.User) || TieneAlcance(context.User, programaId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private bool TieneRolDireccion(ClaimsPrincipal usuario)
        => LectorClaims.Valores(usuario, _opciones.ClaimRol)
            .Contains(_opciones.RolDireccion, StringComparer.OrdinalIgnoreCase);

    private bool TieneAlcance(ClaimsPrincipal usuario, Guid programaId)
        => LectorClaims.Valores(usuario, _opciones.ClaimProgramaIds)
            .Any(valor => Guid.TryParse(valor, out var id) && id == programaId);
}
