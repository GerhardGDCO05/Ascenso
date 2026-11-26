using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Asignaciones.Entidades;

/// <summary>
/// Registro inmutable que documenta la autorización de una excepción de evaluador único
/// para una postulación (RN-POS-04, RF-35, RN-AUD-01).
/// </summary>
public sealed class AutorizacionExcepcionEvaluadorUnico : EntidadInmutable
{
    public const int LongitudMinimaJustificacion = 10;

    public const int LongitudMaximaJustificacion = 1000;

    // Constructor requerido por los adaptadores de persistencia.
    private AutorizacionExcepcionEvaluadorUnico()
    {
    }

    private AutorizacionExcepcionEvaluadorUnico(
        Guid postulacionId,
        string justificacion,
        DateTimeOffset fechaAutorizacion,
        Guid usuarioAutorizadorId,
        Guid correlacionId)
        : base(UuidV7.Nuevo(), fechaAutorizacion, usuarioAutorizadorId, correlacionId)
    {
        PostulacionId = postulacionId;
        Justificacion = justificacion;
    }

    public Guid PostulacionId { get; private set; }

    public string Justificacion { get; private set; } = string.Empty;

    public static AutorizacionExcepcionEvaluadorUnico Crear(
        Guid postulacionId,
        string justificacion,
        DateTimeOffset fechaAutorizacion,
        Guid usuarioAutorizadorId,
        Guid correlacionId)
    {
        if (fechaAutorizacion == default)
        {
            throw new ArgumentException(
                "La fecha de autorización es obligatoria.",
                nameof(fechaAutorizacion));
        }

        if (usuarioAutorizadorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario autorizador es obligatorio.",
                nameof(usuarioAutorizadorId));
        }

        if (correlacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de correlación es obligatorio.",
                nameof(correlacionId));
        }

        if (postulacionId == Guid.Empty)
        {
            throw new AsignacionEvaluacionInvalidaException(
                "El identificador de la postulación es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(justificacion))
        {
            throw new JustificacionExcepcionInvalidaException();
        }

        var justificacionNormalizada = justificacion.Trim();
        if (justificacionNormalizada.Length < LongitudMinimaJustificacion
            || justificacionNormalizada.Length > LongitudMaximaJustificacion)
        {
            throw new JustificacionExcepcionInvalidaException();
        }

        return new AutorizacionExcepcionEvaluadorUnico(
            postulacionId,
            justificacionNormalizada,
            fechaAutorizacion,
            usuarioAutorizadorId,
            correlacionId);
    }
}
