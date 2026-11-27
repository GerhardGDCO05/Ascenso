namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta modificar o promover nuevamente una entrada de lista de espera ya promovida.
/// </summary>
public sealed class EntradaListaEsperaYaPromovidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "ENTRADA_LISTA_ESPERA_YA_PROMOVIDA";

    public EntradaListaEsperaYaPromovidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La entrada de lista de espera ya fue promovida previamente y no admite cambios adicionales."
                : mensaje,
            409)
    {
    }
}
