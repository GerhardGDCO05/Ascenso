namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CarreraElegibleNoEncontradaException : DomainException
{
    public const string CodigoError = "CARRERA_NO_ENCONTRADA";

    public CarreraElegibleNoEncontradaException(Guid carreraId)
        : base(
            CodigoError,
            404,
            $"No se encontró la carrera elegible con identificador '{carreraId}'.")
    {
        CarreraId = carreraId;
    }

    public Guid CarreraId { get; }
}
