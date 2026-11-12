using System.Net;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Transiciones;

namespace Ascenso.Becas.Domain.Otorgamiento.Entidades;

/// <summary>
/// Compromiso firmado del becario. Se modifica únicamente a través de la
/// raíz <c>Beca</c> para mantener sincronizada la habilitación de desembolsos.
/// </summary>
public sealed class CompromisoBecario : EntidadAuditable
{
    private CompromisoBecario()
    {
    }

    private CompromisoBecario(Guid id, Guid becaId, ContextoOperacion contexto)
        : base(id, contexto.Fecha, contexto.UsuarioAutorId)
    {
        BecaId = becaId;
        Estado = EstadoCompromiso.PendienteCarga;
        NumeroIntentoCarga = 0;
    }

    public Guid BecaId { get; private set; }

    public string? UrlPlantillaGeneradaFirebase { get; private set; }

    public string? UrlPdfFirmadoFirebase { get; private set; }

    public string? HashDocumentoSha256 { get; private set; }

    public int NumeroIntentoCarga { get; private set; }

    public string? IpCargaFirma { get; private set; }

    public DateTimeOffset? FechaCargaFirma { get; private set; }

    public EstadoCompromiso Estado { get; private set; }

    public string? ObservacionesRevision { get; private set; }

    public Guid? RevisadoPorCoordinadorId { get; private set; }

    public DateTimeOffset? FechaRevision { get; private set; }

    internal static CompromisoBecario Crear(Guid becaId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        if (becaId == Guid.Empty)
        {
            throw new ArgumentException("La beca del compromiso es obligatoria.", nameof(becaId));
        }

        return new CompromisoBecario(UuidV7.Nuevo(), becaId, contexto);
    }

    internal void Cargar(
        string urlPdfFirmadoFirebase,
        string hashDocumentoSha256,
        string ipCargaFirma,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarTransicion(EstadoCompromiso.EnRevision);

        if (string.IsNullOrWhiteSpace(urlPdfFirmadoFirebase))
        {
            throw new ArgumentException("La URL del PDF firmado es obligatoria.", nameof(urlPdfFirmadoFirebase));
        }

        if (!EsHashSha256Valido(hashDocumentoSha256))
        {
            throw new ArgumentException(
                "El hash del compromiso debe ser un SHA-256 hexadecimal de 64 caracteres.",
                nameof(hashDocumentoSha256));
        }

        if (string.IsNullOrWhiteSpace(ipCargaFirma) || !IPAddress.TryParse(ipCargaFirma, out _))
        {
            throw new ArgumentException("La IP de carga de la firma debe ser válida.", nameof(ipCargaFirma));
        }

        if (Estado == EstadoCompromiso.Rechazado
            && string.Equals(HashDocumentoSha256, hashDocumentoSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El nuevo intento no puede reutilizar el hash del intento rechazado.",
                nameof(hashDocumentoSha256));
        }

        Estado = EstadoCompromiso.EnRevision;
        UrlPdfFirmadoFirebase = urlPdfFirmadoFirebase.Trim();
        HashDocumentoSha256 = hashDocumentoSha256.Trim().ToLowerInvariant();
        IpCargaFirma = ipCargaFirma.Trim();
        FechaCargaFirma = contexto.Fecha;
        NumeroIntentoCarga++;
        ObservacionesRevision = null;
        RevisadoPorCoordinadorId = null;
        FechaRevision = null;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Aprobar(string? observaciones, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarTransicion(EstadoCompromiso.Aprobado);

        Estado = EstadoCompromiso.Aprobado;
        ObservacionesRevision = Normalizar(observaciones);
        RevisadoPorCoordinadorId = contexto.UsuarioAutorId;
        FechaRevision = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Rechazar(string observaciones, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarTransicion(EstadoCompromiso.Rechazado);

        if (string.IsNullOrWhiteSpace(observaciones))
        {
            throw new ArgumentException("Las observaciones del rechazo son obligatorias.", nameof(observaciones));
        }

        Estado = EstadoCompromiso.Rechazado;
        ObservacionesRevision = observaciones.Trim();
        RevisadoPorCoordinadorId = contexto.UsuarioAutorId;
        FechaRevision = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    private void ValidarTransicion(EstadoCompromiso destino)
    {
        if (TransicionesCompromisoBecario.Tabla.EsTerminal(Estado))
        {
            throw new TransicionEstadoInvalidaException(Estado, destino);
        }

        TransicionesCompromisoBecario.Tabla.Validar(Estado, destino);
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static bool EsHashSha256Valido(string hash)
    {
        if (hash is null || hash.Length != 64)
        {
            return false;
        }

        foreach (var caracter in hash)
        {
            if (!Uri.IsHexDigit(caracter))
            {
                return false;
            }
        }

        return true;
    }
}
