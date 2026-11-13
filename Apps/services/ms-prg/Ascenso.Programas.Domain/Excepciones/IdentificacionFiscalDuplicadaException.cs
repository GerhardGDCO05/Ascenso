namespace Ascenso.Programas.Domain.Excepciones;

public sealed class IdentificacionFiscalDuplicadaException : DomainException
{
    public const string CodigoError = "IDENTIFICACION_FISCAL_DUPLICADA";

    public IdentificacionFiscalDuplicadaException(string identificacionFiscal)
        : base(
            CodigoError,
            409,
            $"Ya existe un donante registrado con la identificación fiscal '{identificacionFiscal}'.")
    {
        IdentificacionFiscal = identificacionFiscal;
    }

    public string IdentificacionFiscal { get; }
}

