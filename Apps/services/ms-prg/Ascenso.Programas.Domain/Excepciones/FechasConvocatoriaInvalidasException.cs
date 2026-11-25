namespace Ascenso.Programas.Domain.Excepciones;

public sealed class FechasConvocatoriaInvalidasException : DomainException
{
    public const string CodigoError = "FECHAS_CONVOCATORIA_INVALIDAS";

    public FechasConvocatoriaInvalidasException(string motivo)
        : base(
            CodigoError,
            400,
            $"La cronología de la convocatoria no es válida: {motivo}")
    {
        Motivo = motivo;
    }

    public string Motivo { get; }
}
