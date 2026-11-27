namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el presupuesto disponible efectivo del programa no alcanza para cubrir el monto estimado de la beca al promover.
/// </summary>
public sealed class PresupuestoInsuficientePromocionException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PRESUPUESTO_INSUFICIENTE_PROMOCION";

    public PresupuestoInsuficientePromocionException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El presupuesto disponible efectivo del programa es insuficiente para cubrir el monto estimado de la beca del candidato."
                : mensaje,
            422)
    {
    }
}
