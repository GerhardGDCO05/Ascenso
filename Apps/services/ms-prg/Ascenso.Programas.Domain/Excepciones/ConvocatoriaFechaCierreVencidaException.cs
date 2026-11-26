namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ConvocatoriaFechaCierreVencidaException : DomainException
{
    public const string CodigoError = "CONVOCATORIA_FECHA_CIERRE_VENCIDA";

    public ConvocatoriaFechaCierreVencidaException(Guid convocatoriaId, DateTimeOffset fechaCierre)
        : base(
            CodigoError,
            422,
            $"La convocatoria '{convocatoriaId}' no puede publicarse porque su fecha de cierre '{fechaCierre:O}' ya venció.")
    {
        ConvocatoriaId = convocatoriaId;
        FechaCierre = fechaCierre;
    }

    public Guid ConvocatoriaId { get; }

    public DateTimeOffset FechaCierre { get; }
}
