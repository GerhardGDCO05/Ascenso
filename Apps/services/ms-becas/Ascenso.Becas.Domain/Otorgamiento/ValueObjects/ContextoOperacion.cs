using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

/// <summary>
/// Contexto de auditoría y trazabilidad de una operación de dominio.
/// </summary>
public sealed record class ContextoOperacion : ValueObject
{
    public ContextoOperacion(
        Guid usuarioAutorId,
        Guid correlacionId,
        DateTimeOffset fecha,
        string? justificacion = null,
        Guid? autorizacionOtpId = null)
    {
        if (usuarioAutorId == Guid.Empty)
        {
            throw new ArgumentException("El usuario autor es obligatorio.", nameof(usuarioAutorId));
        }

        if (correlacionId == Guid.Empty)
        {
            throw new ArgumentException("La correlación es obligatoria.", nameof(correlacionId));
        }

        if (fecha == default)
        {
            throw new ArgumentException("La fecha de la operación es obligatoria.", nameof(fecha));
        }

        if (autorizacionOtpId == Guid.Empty)
        {
            throw new ArgumentException(
                "La autorización OTP, cuando se informa, debe tener un identificador válido.",
                nameof(autorizacionOtpId));
        }

        UsuarioAutorId = usuarioAutorId;
        CorrelacionId = correlacionId;
        Fecha = fecha.ToUniversalTime();
        Justificacion = string.IsNullOrWhiteSpace(justificacion) ? null : justificacion.Trim();
        AutorizacionOtpId = autorizacionOtpId;
    }

    public Guid UsuarioAutorId { get; }

    public Guid CorrelacionId { get; }

    public DateTimeOffset Fecha { get; }

    public string? Justificacion { get; }

    public Guid? AutorizacionOtpId { get; }

    public bool TieneJustificacionSuficiente(int longitudMinima = 10)
        => Justificacion is not null && Justificacion.Length >= longitudMinima;
}
