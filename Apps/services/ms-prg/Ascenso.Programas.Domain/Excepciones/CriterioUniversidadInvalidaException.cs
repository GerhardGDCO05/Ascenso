namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CriterioUniversidadInvalidaException : DomainException
{
    public const string CodigoError = "CRITERIO_UNIVERSIDAD_INVALIDA";

    public CriterioUniversidadInvalidaException(IReadOnlyCollection<Guid> universidadesInvalidas)
        : base(
            CodigoError,
            422,
            $"Las siguientes universidades no pertenecen a los convenios vigentes del programa: {string.Join(", ", universidadesInvalidas)}.")
    {
        UniversidadesInvalidas = universidadesInvalidas;
    }

    public IReadOnlyCollection<Guid> UniversidadesInvalidas { get; }
}
