namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el programa o convocatoria no dispone de cupos efectivos para promover al candidato.
/// </summary>
public sealed class SinCupoDisponiblePromocionException : ExcepcionReglaNegocio
{
    public const string CodigoError = "SIN_CUPO_DISPONIBLE_PROMOCION";

    public SinCupoDisponiblePromocionException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No existen cupos disponibles efectivos en el programa para promover al candidato desde la lista de espera."
                : mensaje,
            409)
    {
    }
}
