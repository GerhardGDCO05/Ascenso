namespace Ascenso.Programas.Domain.Comun;

/// <summary>
/// Representa el resultado de una operación de dominio sin valor de retorno.
/// </summary>
public class Result
{
    protected Result(bool esExitoso, string? codigoError, string? mensajeError)
    {
        if (esExitoso && (!string.IsNullOrWhiteSpace(codigoError) || !string.IsNullOrWhiteSpace(mensajeError)))
        {
            throw new ArgumentException("Un resultado exitoso no puede contener información de error.");
        }

        if (!esExitoso && string.IsNullOrWhiteSpace(codigoError))
        {
            throw new ArgumentException("Un resultado fallido debe especificar el código de error.", nameof(codigoError));
        }

        EsExitoso = esExitoso;
        CodigoError = codigoError;
        MensajeError = mensajeError;
    }

    public bool EsExitoso { get; }

    public bool EsFallo => !EsExitoso;

    public string? CodigoError { get; }

    public string? MensajeError { get; }

    public static Result Exito()
        => new(true, null, null);

    public static Result Fallo(string codigoError, string? mensajeError = null)
        => new(false, codigoError, mensajeError);

    public static Result<TValue> Exito<TValue>(TValue valor)
        => new(valor, true, null, null);

    public static Result<TValue> Fallo<TValue>(string codigoError, string? mensajeError = null)
        => new(default, false, codigoError, mensajeError);
}

/// <summary>
/// Representa el resultado de una operación de dominio con un valor tipado.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _valor;

    internal Result(TValue? valor, bool esExitoso, string? codigoError, string? mensajeError)
        : base(esExitoso, codigoError, mensajeError)
    {
        _valor = valor;
    }

    public TValue Valor => EsExitoso
        ? _valor!
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");
}

