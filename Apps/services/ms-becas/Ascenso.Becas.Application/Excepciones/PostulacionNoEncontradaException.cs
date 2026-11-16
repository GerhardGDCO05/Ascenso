namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando <c>MS-POS</c> informa que la postulación solicitada no
/// existe. El contrato interno la trata como una solicitud no procesable para
/// no confundirla con una caída del proveedor.
/// </summary>
public sealed class PostulacionNoEncontradaException : AplicacionException
{
    public const string CodigoError = "POSTULACION_NO_ENCONTRADA";

    public PostulacionNoEncontradaException(Guid postulacionId)
        : base(
            CodigoError,
            422,
            $"La postulación '{postulacionId}' no existe en el servicio de postulaciones.")
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        PostulacionId = postulacionId;
    }

    public Guid PostulacionId { get; }
}
