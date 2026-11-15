namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Agrupa los errores de validación de una solicitud antes de ejecutar el
/// caso de uso.
/// </summary>
public sealed class ValidacionAplicacionException : AplicacionException
{
    public const string CodigoError = "SOLICITUD_INVALIDA";

    public ValidacionAplicacionException(IReadOnlyDictionary<string, string[]> errores)
        : base(
            CodigoError,
            400,
            "La solicitud contiene errores de validación.")
    {
        ArgumentNullException.ThrowIfNull(errores);

        if (errores.Count == 0)
        {
            throw new ArgumentException(
                "La validación debe contener al menos un error.",
                nameof(errores));
        }

        Errores = errores;
    }

    public IReadOnlyDictionary<string, string[]> Errores { get; }
}
