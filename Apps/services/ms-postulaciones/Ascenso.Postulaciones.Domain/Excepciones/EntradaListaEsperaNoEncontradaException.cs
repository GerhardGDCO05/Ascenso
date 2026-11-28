namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe la entrada de lista de espera solicitada para la convocatoria.
/// </summary>
public sealed class EntradaListaEsperaNoEncontradaException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "ENTRADA_LISTA_ESPERA_NO_ENCONTRADA";

    public EntradaListaEsperaNoEncontradaException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró la entrada de lista de espera solicitada para la convocatoria."
                : mensaje,
            CodigoError)
    {
    }
}
