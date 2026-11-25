using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ConvocatoriaNoEditableException : DomainException
{
    public const string CodigoError = "CONVOCATORIA_NO_EDITABLE";

    public ConvocatoriaNoEditableException(Guid convocatoriaId, EstadoConvocatoria estado)
        : base(
            CodigoError,
            409,
            $"La convocatoria '{convocatoriaId}' está en estado '{estado}' y solo puede modificarse en estado BORRADOR.")
    {
        ConvocatoriaId = convocatoriaId;
        Estado = estado;
    }

    public Guid ConvocatoriaId { get; }

    public EstadoConvocatoria Estado { get; }
}
