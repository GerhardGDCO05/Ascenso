namespace Ascenso.Postulaciones.Application.Interfaces;

/// <summary>
/// Puerto ISP para consultar los roles asignados al usuario autenticado actual.
/// </summary>
public interface IRolesUsuarioActual
{
    bool TieneRol(string rol);
}
