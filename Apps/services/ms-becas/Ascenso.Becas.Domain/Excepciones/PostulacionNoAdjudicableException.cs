namespace Ascenso.Becas.Domain.Excepciones;

public sealed class PostulacionNoAdjudicableException : DomainException
{
    public const string CodigoError = "POSTULACION_NO_ADJUDICABLE";

    public PostulacionNoAdjudicableException(Guid postulacionId, string? motivo = null)
        : base(
            CodigoError,
            422,
            motivo is null
                ? $"La postulación '{postulacionId}' no es adjudicable."
                : $"La postulación '{postulacionId}' no es adjudicable: {motivo}")
    {
        PostulacionId = postulacionId;
        Motivo = motivo;
    }

    public Guid PostulacionId { get; }

    public string? Motivo { get; }
}
