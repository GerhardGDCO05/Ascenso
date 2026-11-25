namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CuposInsuficientesException : DomainException
{
    public const string CodigoError = "CUPOS_INSUFICIENTES";

    public CuposInsuficientesException(Guid programaId, int solicitados, int disponibles)
        : base(
            CodigoError,
            422,
            $"El programa '{programaId}' solo tiene {disponibles} cupos disponibles y la convocatoria solicita {solicitados}.")
    {
        ProgramaId = programaId;
        Solicitados = solicitados;
        Disponibles = disponibles;
    }

    public Guid ProgramaId { get; }

    public int Solicitados { get; }

    public int Disponibles { get; }
}
