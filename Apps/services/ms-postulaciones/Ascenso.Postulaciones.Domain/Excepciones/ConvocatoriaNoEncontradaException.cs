namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la convocatoria asociada no existe en MS-PRG.
/// </summary>
public sealed class ConvocatoriaNoEncontradaException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "CONVOCATORIA_NO_ENCONTRADA";

    public ConvocatoriaNoEncontradaException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró la convocatoria asociada a la evaluación."
                : mensaje,
            CodigoError)
    {
    }
}
