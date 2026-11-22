using System.Globalization;
using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta modificar o publicar una rúbrica que ya está publicada o bloqueada.
/// </summary>
public sealed class RubricaBloqueadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RUBRICA_BLOQUEADA";

    public RubricaBloqueadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La rúbrica de evaluación no admite modificaciones ni nueva publicación en su estado actual."
                : mensaje,
            409)
    {
    }

    public static RubricaBloqueadaException Publicada()
        => new("La rúbrica está publicada y no admite modificaciones; los cambios requieren una rúbrica nueva.");

    public static RubricaBloqueadaException Bloqueada(
        DateTimeOffset fechaBloqueo,
        MotivoBloqueoRubrica motivo)
    {
        var motivoLegible = motivo switch
        {
            MotivoBloqueoRubrica.InicioEvaluacion => "inicio de la etapa de evaluación",
            MotivoBloqueoRubrica.PrimeraCalificacion => "registro de la primera calificación",
            _ => throw new ArgumentOutOfRangeException(
                nameof(motivo),
                motivo,
                "El motivo de bloqueo de la rúbrica no es válido.")
        };

        var fechaUtc = fechaBloqueo
            .ToUniversalTime()
            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        return new RubricaBloqueadaException(
            $"La rúbrica está bloqueada desde {fechaUtc} UTC por {motivoLegible}; los cambios requieren una rúbrica nueva.");
    }
}
