namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ProgramaBecaNoEncontradoException : DomainException
{
    public const string CodigoError = "PROGRAMA_BECA_NO_ENCONTRADO";

    public ProgramaBecaNoEncontradoException(Guid programaId)
        : base(
            CodigoError,
            404,
            $"No se encontró el programa de becas con identificador '{programaId}'.")
    {
        ProgramaId = programaId;
    }

    public Guid ProgramaId { get; }
}
