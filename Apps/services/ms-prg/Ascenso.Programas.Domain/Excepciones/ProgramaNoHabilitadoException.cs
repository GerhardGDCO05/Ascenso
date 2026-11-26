namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ProgramaNoHabilitadoException : DomainException
{
    public const string CodigoError = "PROGRAMA_NO_HABILITADO";

    public ProgramaNoHabilitadoException(Guid programaId, string motivo)
        : base(
            CodigoError,
            422,
            $"El programa de becas '{programaId}' no está habilitado para convocar postulaciones: {motivo}")
    {
        ProgramaId = programaId;
        Motivo = motivo;
    }

    public Guid ProgramaId { get; }

    public string Motivo { get; }
}
