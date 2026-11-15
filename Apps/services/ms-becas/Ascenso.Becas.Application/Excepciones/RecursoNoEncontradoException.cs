namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando el recurso solicitado no existe o no es visible para el
/// caso de uso.
/// </summary>
public sealed class RecursoNoEncontradoException : AplicacionException
{
    public const string CodigoError = "RECURSO_NO_ENCONTRADO";

    public RecursoNoEncontradoException(string recurso, Guid identificador)
        : base(
            CodigoError,
            404,
            $"No se encontró el recurso '{recurso}' con identificador '{identificador}'.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recurso);
        Recurso = recurso;
        Identificador = identificador;
    }

    public string Recurso { get; }

    public Guid Identificador { get; }
}
