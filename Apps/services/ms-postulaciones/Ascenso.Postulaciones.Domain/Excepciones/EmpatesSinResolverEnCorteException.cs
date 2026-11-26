namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando existen empates reales de puntaje en la línea de corte que impiden confirmar el ranking (RN-POS-07).
/// </summary>
public sealed class EmpatesSinResolverEnCorteException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EMPATES_SIN_RESOLVER";

    public EmpatesSinResolverEnCorteException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Existen empates sin resolver en la línea de corte que impiden confirmar el ranking de la convocatoria."
                : mensaje,
            422)
    {
    }
}
