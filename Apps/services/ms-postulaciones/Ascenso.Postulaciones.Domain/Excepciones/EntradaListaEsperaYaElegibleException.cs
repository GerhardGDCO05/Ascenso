namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta marcar como elegible por vacante una entrada que ya se encuentra en dicho estado.
/// </summary>
public sealed class EntradaListaEsperaYaElegibleException : ExcepcionReglaNegocio
{
    public const string CodigoError = "ENTRADA_LISTA_ESPERA_YA_ELEGIBLE";

    public EntradaListaEsperaYaElegibleException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La entrada de lista de espera ya se encuentra marcada como elegible por vacante."
                : mensaje,
            409)
    {
    }
}
