namespace Ascenso.Becas.Domain.Excepciones;

public sealed class JustificacionRequeridaException : DomainException
{
    public const string CodigoError = "JUSTIFICACION_REQUERIDA";

    public JustificacionRequeridaException(string operacion, int longitudMinima = 10)
        : base(
            CodigoError,
            400,
            $"La operación '{operacion}' requiere una justificación de al menos {longitudMinima} caracteres.")
    {
        if (string.IsNullOrWhiteSpace(operacion))
        {
            throw new ArgumentException("La operación es obligatoria.", nameof(operacion));
        }

        if (longitudMinima < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(longitudMinima), "La longitud mínima debe ser mayor que cero.");
        }

        Operacion = operacion;
        LongitudMinima = longitudMinima;
    }

    public string Operacion { get; }

    public int LongitudMinima { get; }
}
