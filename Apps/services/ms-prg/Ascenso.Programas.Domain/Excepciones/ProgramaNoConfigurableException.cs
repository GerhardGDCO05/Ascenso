namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ProgramaNoConfigurableException : DomainException
{
    public const string CodigoError = "PROGRAMA_NO_CONFIGURABLE";

    public ProgramaNoConfigurableException(Guid programaId, string motivo)
        : base(
            CodigoError,
            422,
            $"El programa '{programaId}' no cumple las condiciones para pasar a estado Configurado (RN-PRG-01): {motivo}")
    {
        ProgramaId = programaId;
    }

    public Guid ProgramaId { get; }
}
