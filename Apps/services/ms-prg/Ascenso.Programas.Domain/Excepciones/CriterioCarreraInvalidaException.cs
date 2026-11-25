namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CriterioCarreraInvalidaException : DomainException
{
    public const string CodigoError = "CRITERIO_CARRERA_INVALIDA";

    public CriterioCarreraInvalidaException(IReadOnlyCollection<Guid> carrerasInvalidas)
        : base(
            CodigoError,
            422,
            $"Las siguientes carreras no pertenecen a los convenios vigentes del programa: {string.Join(", ", carrerasInvalidas)}.")
    {
        CarrerasInvalidas = carrerasInvalidas;
    }

    public IReadOnlyCollection<Guid> CarrerasInvalidas { get; }
}
