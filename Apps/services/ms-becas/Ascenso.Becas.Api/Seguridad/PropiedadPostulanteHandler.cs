using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Autoriza al postulante o becario cuando el claim <c>postulante_id</c>
/// coincide con el dueño del recurso. Sin dueño identificado se falla cerrado.
/// </summary>
public sealed class PropiedadPostulanteHandler : AuthorizationHandler<PropiedadPostulanteRequirement>
{
    private readonly AutorizacionOptions _opciones;

    public PropiedadPostulanteHandler(IOptions<AutorizacionOptions> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _opciones = opciones.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PropiedadPostulanteRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.Resource is not IRecursoAutorizable recurso
            || recurso.PostulanteId is not { } postulanteId
            || postulanteId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        var valorClaim = LectorClaims.PrimerValor(
            context.User,
            _opciones.ClaimPostulanteId);

        if (Guid.TryParse(valorClaim, out var postulanteDelToken)
            && postulanteDelToken == postulanteId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
