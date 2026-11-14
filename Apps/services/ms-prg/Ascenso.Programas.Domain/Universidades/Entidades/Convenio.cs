using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Universidades.Entidades;

/// <summary>
/// Convenio interinstitucional suscrito con una universidad aliada y respaldado por PDF en Firebase Storage.
/// </summary>
public sealed class Convenio : EntidadAuditable
{
    private Convenio()
    {
    }

    private Convenio(
        Guid id,
        Guid universidadAliadaId,
        string numeroConvenio,
        string terminosResumen,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string urlDocumentoPdfFirebase,
        bool permiteIntercambioAutomaticoDatos,
        EstadoConvenio estado,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        UniversidadAliadaId = universidadAliadaId;
        NumeroConvenio = numeroConvenio;
        TerminosResumen = terminosResumen;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        UrlDocumentoPdfFirebase = urlDocumentoPdfFirebase;
        PermiteIntercambioAutomaticoDatos = permiteIntercambioAutomaticoDatos;
        Estado = estado;
    }

    public Guid UniversidadAliadaId { get; private set; }

    public string NumeroConvenio { get; private set; } = string.Empty;

    public string TerminosResumen { get; private set; } = string.Empty;

    public DateOnly FechaInicio { get; private set; }

    public DateOnly FechaFin { get; private set; }

    public string UrlDocumentoPdfFirebase { get; private set; } = string.Empty;

    public bool PermiteIntercambioAutomaticoDatos { get; private set; }

    public EstadoConvenio Estado { get; private set; }

    internal static Convenio CrearVigente(
        Guid universidadAliadaId,
        string numeroConvenio,
        string terminosResumen,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string urlDocumentoPdfFirebase,
        bool permiteIntercambioAutomaticoDatos,
        Guid usuarioCreacionId,
        DateTimeOffset fechaCreacion)
    {
        if (universidadAliadaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la universidad aliada es obligatorio.", nameof(universidadAliadaId));
        }

        if (string.IsNullOrWhiteSpace(numeroConvenio))
        {
            throw new ArgumentException("El número de convenio es obligatorio.", nameof(numeroConvenio));
        }

        if (string.IsNullOrWhiteSpace(terminosResumen))
        {
            throw new ArgumentException("El resumen de términos del convenio es obligatorio.", nameof(terminosResumen));
        }

        if (fechaFin <= fechaInicio)
        {
            throw new FechasConvenioInvalidasException(fechaInicio, fechaFin);
        }

        if (!EsRutaPdfValida(urlDocumentoPdfFirebase))
        {
            throw new DocumentoConvenioPdfRequeridoException();
        }

        return new Convenio(
            UuidV7.Nuevo(fechaCreacion),
            universidadAliadaId,
            numeroConvenio.Trim().ToUpperInvariant(),
            terminosResumen.Trim(),
            fechaInicio,
            fechaFin,
            urlDocumentoPdfFirebase.Trim(),
            permiteIntercambioAutomaticoDatos,
            EstadoConvenio.Vigente,
            fechaCreacion,
            usuarioCreacionId);
    }

    internal bool EstaVigenteEn(DateOnly fechaReferencia)
        => Estado == EstadoConvenio.Vigente && FechaFin >= fechaReferencia;

    internal void MarcarVencido(Guid usuarioModificacionId, DateTimeOffset fechaModificacion)
    {
        if (Estado == EstadoConvenio.Vencido)
        {
            return;
        }

        Estado = EstadoConvenio.Vencido;
        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);
    }

    internal void Suspender(Guid usuarioModificacionId, DateTimeOffset fechaModificacion)
    {
        if (Estado == EstadoConvenio.Vencido)
        {
            throw new InvalidOperationException("No es posible suspender un convenio que ya se encuentra vencido.");
        }

        Estado = EstadoConvenio.Suspendido;
        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);
    }

    private static bool EsRutaPdfValida(string? urlDocumento)
    {
        if (string.IsNullOrWhiteSpace(urlDocumento))
        {
            return false;
        }

        var limpia = urlDocumento.Trim();
        var indiceQuery = limpia.IndexOfAny(['?', '#']);
        if (indiceQuery >= 0)
        {
            limpia = limpia[..indiceQuery];
        }

        return limpia.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }
}
