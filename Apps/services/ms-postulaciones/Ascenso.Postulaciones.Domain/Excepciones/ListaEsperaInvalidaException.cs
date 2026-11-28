namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los datos o invariantes de dominio de la lista de espera no son válidos.
/// </summary>
public sealed class ListaEsperaInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "LISTA_ESPERA_INVALIDA";

    public ListaEsperaInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Los datos proporcionados para la entrada de lista de espera no son válidos."
                : mensaje,
            422)
    {
    }
}
