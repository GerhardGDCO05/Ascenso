namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando las postulaciones seleccionadas para resolver un caso límite exceden los cupos o el presupuesto efectivos restantes.
/// </summary>
public sealed class SeleccionCasoLimiteExcedeDisponibilidadException : ExcepcionReglaNegocio
{
    public const string CodigoError = "SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD";

    public SeleccionCasoLimiteExcedeDisponibilidadException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Las postulaciones seleccionadas exceden los cupos o el presupuesto efectivo disponible para el caso límite."
                : mensaje,
            422)
    {
    }
}
