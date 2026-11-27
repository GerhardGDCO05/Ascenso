namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta registrar más de una entrada de lista de espera para la misma postulación.
/// </summary>
public sealed class EntradaListaEsperaDuplicadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "ENTRADA_LISTA_ESPERA_DUPLICADA";

    public EntradaListaEsperaDuplicadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Ya existe una entrada registrada en lista de espera para la postulación indicada."
                : mensaje,
            409)
    {
    }
}
