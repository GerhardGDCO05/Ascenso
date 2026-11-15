namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando la solicitud es sintácticamente válida pero no puede
/// ejecutarse por una precondición de negocio de la capa de aplicación.
/// </summary>
public sealed class SolicitudInvalidaException : AplicacionException
{
    public const string CodigoError = "SOLICITUD_INVALIDA";

    public SolicitudInvalidaException(string detalle)
        : base(CodigoError, 400, detalle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detalle);
    }
}
