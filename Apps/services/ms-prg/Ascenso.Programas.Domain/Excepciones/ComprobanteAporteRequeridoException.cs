namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ComprobanteAporteRequeridoException : DomainException
{
    public const string CodigoError = "COMPROBANTE_REQUERIDO";

    public ComprobanteAporteRequeridoException(string? mensaje = null)
        : base(
            CodigoError,
            400,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Todo aporte financiero exige un comprobante válido cargado en Firebase Storage (RN-PRG-04)."
                : mensaje)
    {
    }
}

