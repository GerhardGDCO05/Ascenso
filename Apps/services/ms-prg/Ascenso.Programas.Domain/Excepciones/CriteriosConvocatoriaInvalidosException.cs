namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CriteriosConvocatoriaInvalidosException : DomainException
{
    public const string CodigoError = "CRITERIOS_CONVOCATORIA_INVALIDOS";

    public CriteriosConvocatoriaInvalidosException(string motivo)
        : base(
            CodigoError,
            400,
            $"Los criterios de elegibilidad de la convocatoria no son válidos: {motivo}")
    {
        Motivo = motivo;
    }

    public string Motivo { get; }
}
