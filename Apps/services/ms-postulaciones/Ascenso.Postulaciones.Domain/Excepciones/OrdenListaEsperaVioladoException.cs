namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta promover a un candidato saltándose a otro con menor posición aún pendiente en cola.
/// </summary>
public sealed class OrdenListaEsperaVioladoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "ORDEN_LISTA_ESPERA_VIOLADO";

    public OrdenListaEsperaVioladoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No es posible promover al candidato solicitado porque existen postulantes anteriores pendientes en la lista de espera."
                : mensaje,
            409)
    {
    }
}
