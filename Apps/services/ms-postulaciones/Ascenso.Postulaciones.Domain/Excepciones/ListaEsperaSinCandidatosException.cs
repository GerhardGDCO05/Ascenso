namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la lista de espera de la convocatoria no tiene candidatos disponibles para la operación solicitada.
/// </summary>
public sealed class ListaEsperaSinCandidatosException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "LISTA_ESPERA_SIN_CANDIDATOS";

    public ListaEsperaSinCandidatosException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No existen candidatos pendientes disponibles en la lista de espera de la convocatoria."
                : mensaje,
            CodigoError)
    {
    }
}
