using Microsoft.AspNetCore.Authorization;

namespace Ascenso.Programas.Api.Seguridad;

/// <summary>
/// Requisito PBAC: el token debe incluir el permiso indicado en el claim <c>permissions</c>.
/// </summary>
public sealed class RequisitoPermiso : IAuthorizationRequirement
{
    public RequisitoPermiso(string permiso)
    {
        if (string.IsNullOrWhiteSpace(permiso))
        {
            throw new ArgumentException("El permiso es obligatorio.", nameof(permiso));
        }

        Permiso = permiso.Trim();
    }

    public string Permiso { get; }
}
