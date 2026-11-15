using Ascenso.Becas.Application.Excepciones;

namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Se produce cuando no existe un handler registrado para una solicitud.
/// Es un error de configuración del servicio, no de la petición.
/// </summary>
public sealed class HandlerNoEncontradoException : AplicacionException
{
    public const string CodigoError = "HANDLER_NO_ENCONTRADO";

    public HandlerNoEncontradoException(Type tipoSolicitud)
        : base(
            CodigoError,
            500,
            $"No se encontró un handler registrado para la solicitud '{tipoSolicitud.FullName}'.")
    {
        ArgumentNullException.ThrowIfNull(tipoSolicitud);
        TipoSolicitud = tipoSolicitud;
    }

    public Type TipoSolicitud { get; }
}
