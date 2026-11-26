using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Comites.Entidades;

/// <summary>
/// Membresía de un evaluador interno o externo dentro de un comité evaluador.
/// </summary>
public sealed class MiembroComite : EntidadAuditable
{
    // Constructor requerido por los adaptadores de persistencia.
    private MiembroComite()
    {
    }

    private MiembroComite(
        Guid comiteEvaluadorId,
        Guid usuarioEvaluadorId,
        TipoEvaluador tipo,
        RolMiembroComite rol,
        DateTimeOffset fechaAsignacion,
        Guid usuarioCreacionId)
        : base(UuidV7.Nuevo(), fechaAsignacion, usuarioCreacionId)
    {
        ComiteEvaluadorId = comiteEvaluadorId;
        UsuarioEvaluadorId = usuarioEvaluadorId;
        Tipo = tipo;
        Rol = rol;
        FechaAsignacion = DateOnly.FromDateTime(FechaCreacion.UtcDateTime);
        FechaDesvinculacion = null;
        Activo = true;
    }

    public Guid ComiteEvaluadorId { get; private set; }

    public Guid UsuarioEvaluadorId { get; private set; }

    public TipoEvaluador Tipo { get; private set; }

    public RolMiembroComite Rol { get; private set; }

    public DateOnly FechaAsignacion { get; private set; }

    public DateOnly? FechaDesvinculacion { get; private set; }

    public bool Activo { get; private set; }

    public static MiembroComite Crear(
        Guid comiteEvaluadorId,
        Guid usuarioEvaluadorId,
        TipoEvaluador tipo,
        RolMiembroComite rol,
        DateTimeOffset fechaAsignacion,
        Guid usuarioCreacionId)
    {
        if (comiteEvaluadorId == Guid.Empty)
        {
            throw new ComiteEvaluadorInvalidoException(
                "El miembro del comité debe estar asociado a un comité evaluador.");
        }

        if (usuarioEvaluadorId == Guid.Empty)
        {
            throw new ComiteEvaluadorInvalidoException(
                "El identificador del usuario evaluador es obligatorio.");
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de evaluador no es válido.");
        }

        if (!Enum.IsDefined(rol))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rol),
                rol,
                "El rol del miembro del comité no es válido.");
        }

        if (rol == RolMiembroComite.Jefatura && tipo != TipoEvaluador.Interno)
        {
            throw new ComiteEvaluadorInvalidoException(
                "La jefatura del comité evaluador debe ser un evaluador interno.");
        }

        return new MiembroComite(
            comiteEvaluadorId,
            usuarioEvaluadorId,
            tipo,
            rol,
            fechaAsignacion,
            usuarioCreacionId);
    }

    internal bool Desvincular(DateTimeOffset fechaDesvinculacion, Guid usuarioModificacionId)
    {
        if (fechaDesvinculacion == default)
        {
            throw new ArgumentException(
                "La fecha de desvinculación es obligatoria.",
                nameof(fechaDesvinculacion));
        }

        if (usuarioModificacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario de modificación es obligatorio.",
                nameof(usuarioModificacionId));
        }

        if (Rol == RolMiembroComite.Jefatura)
        {
            throw new JefaturaNoDesvinculableException();
        }

        if (!Activo)
        {
            return false;
        }

        var fechaDesvinculacionUtc = DateOnly.FromDateTime(fechaDesvinculacion.UtcDateTime);
        if (fechaDesvinculacionUtc < FechaAsignacion)
        {
            throw new ComiteEvaluadorInvalidoException(
                "La fecha de desvinculación no puede ser anterior a la fecha de asignación.");
        }

        ActualizarAuditoria(usuarioModificacionId, fechaDesvinculacion);
        Activo = false;
        FechaDesvinculacion = fechaDesvinculacionUtc;

        return true;
    }
}
