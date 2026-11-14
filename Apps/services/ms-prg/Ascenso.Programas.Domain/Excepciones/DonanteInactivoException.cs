namespace Ascenso.Programas.Domain.Excepciones;

public sealed class DonanteInactivoException : DomainException
{
    public const string CodigoError = "DONANTE_INACTIVO";

    public DonanteInactivoException(Guid donanteId)
        : base(
            CodigoError,
            409,
            $"El donante '{donanteId}' se encuentra inactivo y no admite registrar nuevos aportes ni compromisos financieros.")
    {
        DonanteId = donanteId;
    }

    public Guid DonanteId { get; }
}

