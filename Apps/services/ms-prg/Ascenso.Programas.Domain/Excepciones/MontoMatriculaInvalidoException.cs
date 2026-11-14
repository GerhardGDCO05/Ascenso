namespace Ascenso.Programas.Domain.Excepciones;

public sealed class MontoMatriculaInvalidoException : DomainException
{
    public const string CodigoError = "MONTO_MATRICULA_INVALIDO";

    public MontoMatriculaInvalidoException(decimal monto)
        : base(
            CodigoError,
            400,
            $"El monto de referencia de matrícula ({monto}) debe ser mayor a cero y tener como máximo dos decimales.")
    {
        Monto = monto;
    }

    public decimal Monto { get; }
}
