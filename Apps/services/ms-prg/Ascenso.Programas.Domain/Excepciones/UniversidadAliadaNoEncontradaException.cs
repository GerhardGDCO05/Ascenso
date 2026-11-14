namespace Ascenso.Programas.Domain.Excepciones;

public sealed class UniversidadAliadaNoEncontradaException : DomainException
{
    public const string CodigoError = "UNIVERSIDAD_NO_ENCONTRADA";

    public UniversidadAliadaNoEncontradaException(Guid universidadId)
        : base(
            CodigoError,
            404,
            $"No se encontró la universidad aliada con identificador '{universidadId}'.")
    {
        UniversidadId = universidadId;
    }

    public Guid UniversidadId { get; }
}
