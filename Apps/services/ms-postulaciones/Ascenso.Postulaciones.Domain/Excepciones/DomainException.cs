namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Base de las excepciones de dominio. Expone el código de negocio y el
/// estatus HTTP con el que el adaptador primario debe responder.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string codigo, int estadoHttp, string mensaje)
        : base(mensaje)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código de error es obligatorio.", nameof(codigo));
        }

        if (estadoHttp is < 400 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(estadoHttp), "El estatus HTTP debe estar entre 400 y 599.");
        }

        Codigo = codigo;
        EstadoHttp = estadoHttp;
    }

    public string Codigo { get; }

    public int EstadoHttp { get; }
}

