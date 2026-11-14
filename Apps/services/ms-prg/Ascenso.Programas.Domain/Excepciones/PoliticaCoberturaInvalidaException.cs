namespace Ascenso.Programas.Domain.Excepciones;

public sealed class PoliticaCoberturaInvalidaException : DomainException
{
    public const string CodigoError = "POLITICA_COBERTURA_INVALIDA";

    public PoliticaCoberturaInvalidaException(string detalle)
        : base(CodigoError, 400, detalle)
    {
    }
}
