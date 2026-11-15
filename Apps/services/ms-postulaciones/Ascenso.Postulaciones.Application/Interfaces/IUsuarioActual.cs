namespace Ascenso.Postulaciones.Application.Interfaces;

/// <summary>
/// Representa al usuario autenticado que ejecuta una operación de la aplicación.
/// </summary>
public interface IUsuarioActual
{
    bool EstaAutenticado { get; }

    Guid UsuarioId { get; }
}
