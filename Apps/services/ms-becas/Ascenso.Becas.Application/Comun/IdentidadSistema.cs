namespace Ascenso.Becas.Application.Comun;

/// <summary>
/// Identidad técnica usada por los trabajos en segundo plano cuando no existe
/// un usuario autenticado en el contexto de ejecución.
/// </summary>
public static class IdentidadSistema
{
    public static readonly Guid UsuarioId =
        new("0190f0a0-0000-7000-8000-000000000001");
}
