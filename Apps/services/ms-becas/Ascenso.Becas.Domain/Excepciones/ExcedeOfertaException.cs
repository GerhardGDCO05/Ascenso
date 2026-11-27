namespace Ascenso.Becas.Domain.Excepciones;

public sealed class ExcedeOfertaException : DomainException
{
    public const string CodigoError = "EXCEDE_OFERTA";

    public ExcedeOfertaException(Guid becaId)
        : base(CodigoError, 422,
            $"El desembolso supera el saldo o el monto aprobado de la beca '{becaId}'.")
    {
    }
}
