using Microsoft.AspNetCore.Authorization;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Exige evidencia de segundo factor mediante <c>acr</c> o <c>amr</c>.
/// </summary>
public sealed class Requiere2FARequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Exige que el usuario tenga alcance sobre los programas indicados en la ruta
/// o en el recurso autorizado.
/// </summary>
public sealed class AlcanceProgramaRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Exige que el usuario sea dueño del postulante indicado en la ruta o en el
/// recurso autorizado.
/// </summary>
public sealed class PropiedadPostulanteRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Exige un <em>Service Token</em> de un cliente permitido con el alcance
/// interno de becas.
/// </summary>
public sealed class CuentaServicioRequirement : IAuthorizationRequirement
{
}
