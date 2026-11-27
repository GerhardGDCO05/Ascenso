namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando un usuario autenticado no posee rol de coordinador de programa ni la jefatura activa del comité evaluador.
/// </summary>
public sealed class PermisoDenegadoResolucionCasoLimiteException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PERMISO_DENEGADO";

    public PermisoDenegadoResolucionCasoLimiteException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Solo el coordinador de programa o la jefatura activa del comité evaluador pueden resolver casos límite del ranking."
                : mensaje,
            403)
    {
    }
}
