using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Excepciones;

public sealed class EstadoProgramaInvalidoParaOperacionException : DomainException
{
    public const string CodigoError = "ESTADO_PROGRAMA_INVALIDO_PARA_OPERACION";

    public EstadoProgramaInvalidoParaOperacionException(Guid programaId, EstadoProgramaBeca estadoActual, string operacion)
        : base(
            CodigoError,
            409,
            $"El programa '{programaId}' en estado '{estadoActual}' no permite ejecutar la operación '{operacion}'.")
    {
        ProgramaId = programaId;
        EstadoActual = estadoActual;
        Operacion = operacion;
    }

    public Guid ProgramaId { get; }

    public EstadoProgramaBeca EstadoActual { get; }

    public string Operacion { get; }
}
