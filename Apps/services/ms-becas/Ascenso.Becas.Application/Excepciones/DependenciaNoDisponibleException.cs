namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando una dependencia externa no responde o devuelve una
/// respuesta inválida.
/// </summary>
public sealed class DependenciaNoDisponibleException : AplicacionException
{
    public const string CodigoError = "DEPENDENCIA_NO_DISPONIBLE";

    public DependenciaNoDisponibleException(string dependencia, Exception? inner = null)
        : base(
            CodigoError,
            503,
            $"La dependencia '{dependencia}' no está disponible en este momento.",
            inner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencia);
        Dependencia = dependencia;
    }

    public string Dependencia { get; }
}
