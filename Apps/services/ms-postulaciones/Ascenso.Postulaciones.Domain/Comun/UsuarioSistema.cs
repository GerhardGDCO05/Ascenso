namespace Ascenso.Postulaciones.Domain.Comun;

/// <summary>
/// Identidad fija del actor del sistema utilizada para operaciones internas de auditoría.
/// </summary>
public static class UsuarioSistema
{
    public static readonly Guid Id = new("00000000-0000-0000-0000-000000000001");
}
