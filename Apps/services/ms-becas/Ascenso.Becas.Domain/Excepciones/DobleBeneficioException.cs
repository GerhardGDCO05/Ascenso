namespace Ascenso.Becas.Domain.Excepciones;

public sealed class DobleBeneficioException : DomainException
{
    public const string CodigoError = "DOBLE_BENEFICIO";

    public DobleBeneficioException(Guid estudianteId, string detalle)
        : base(
            CodigoError,
            409,
            $"El estudiante '{estudianteId}' no puede recibir la beca: {detalle}")
    {
        if (string.IsNullOrWhiteSpace(detalle))
        {
            throw new ArgumentException("El detalle del doble beneficio es obligatorio.", nameof(detalle));
        }

        EstudianteId = estudianteId;
        Detalle = detalle;
    }

    public Guid EstudianteId { get; }

    public string Detalle { get; }
}
