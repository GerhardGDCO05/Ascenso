using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Asignaciones.Agregados;

/// <summary>
/// Raíz de agregado que representa la asignación de una postulación a un miembro activo
/// de un comité evaluador para su evaluación independiente.
/// </summary>
public sealed class AsignacionEvaluacion : AggregateRoot
{
    // Constructor requerido por los adaptadores de persistencia.
    private AsignacionEvaluacion()
    {
    }

    private AsignacionEvaluacion(
        Guid comiteEvaluadorId,
        Guid convocatoriaId,
        Guid postulacionId,
        Guid miembroComiteId,
        Guid usuarioEvaluadorId,
        DateTimeOffset fechaAsignacion,
        DateTimeOffset fechaLimiteEvaluacion,
        Guid usuarioAsignaId)
        : base(UuidV7.Nuevo(), fechaAsignacion, usuarioAsignaId)
    {
        ComiteEvaluadorId = comiteEvaluadorId;
        ConvocatoriaId = convocatoriaId;
        PostulacionId = postulacionId;
        MiembroComiteId = miembroComiteId;
        UsuarioEvaluadorId = usuarioEvaluadorId;
        FechaAsignacion = FechaCreacion;
        FechaLimiteEvaluacion = fechaLimiteEvaluacion.ToUniversalTime();
        Estado = EstadoAsignacionEvaluacion.Pendiente;
    }

    public Guid ComiteEvaluadorId { get; private set; }

    public Guid ConvocatoriaId { get; private set; }

    public Guid PostulacionId { get; private set; }

    public Guid MiembroComiteId { get; private set; }

    public Guid UsuarioEvaluadorId { get; private set; }

    public DateTimeOffset FechaAsignacion { get; private set; }

    public DateTimeOffset FechaLimiteEvaluacion { get; private set; }

    public EstadoAsignacionEvaluacion Estado { get; private set; }

    public static AsignacionEvaluacion Crear(
        ComiteEvaluador comite,
        Guid miembroComiteId,
        Guid postulacionId,
        DateTimeOffset fechaAsignacion,
        DateTimeOffset fechaLimiteEvaluacion,
        Guid usuarioAsignaId)
    {
        ArgumentNullException.ThrowIfNull(comite);

        if (fechaAsignacion == default)
        {
            throw new ArgumentException(
                "La fecha de asignación es obligatoria.",
                nameof(fechaAsignacion));
        }

        if (usuarioAsignaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario que realiza la asignación es obligatorio.",
                nameof(usuarioAsignaId));
        }

        if (!comite.Activo)
        {
            throw new ComiteInactivoException();
        }

        if (postulacionId == Guid.Empty)
        {
            throw new AsignacionEvaluacionInvalidaException(
                "El identificador de la postulación es obligatorio.");
        }

        if (fechaLimiteEvaluacion == default || fechaLimiteEvaluacion <= fechaAsignacion)
        {
            throw new AsignacionEvaluacionInvalidaException(
                "La fecha límite de evaluación debe ser posterior a la fecha de asignación.");
        }

        var miembro = comite.Miembros.FirstOrDefault(item => item.Id == miembroComiteId);
        if (miembro is null || !miembro.Activo)
        {
            throw new EvaluadorNoMiembroActivoException();
        }

        return new AsignacionEvaluacion(
            comite.Id,
            comite.ConvocatoriaId,
            postulacionId,
            miembro.Id,
            miembro.UsuarioEvaluadorId,
            fechaAsignacion,
            fechaLimiteEvaluacion,
            usuarioAsignaId);
    }

    public bool IniciarEvaluacion(DateTimeOffset fechaModificacion, Guid usuarioEvaluadorId)
    {
        ValidarParametrosTransicion(fechaModificacion, usuarioEvaluadorId);

        if (usuarioEvaluadorId != UsuarioEvaluadorId)
        {
            throw new EvaluacionConfidencialException();
        }

        switch (Estado)
        {
            case EstadoAsignacionEvaluacion.Pendiente:
                ActualizarAuditoria(usuarioEvaluadorId, fechaModificacion);
                Estado = EstadoAsignacionEvaluacion.EnProgreso;
                return true;

            case EstadoAsignacionEvaluacion.EnProgreso:
                return false;

            case EstadoAsignacionEvaluacion.Completada:
                throw new EvaluacionYaEnviadaException();

            default:
                throw new AsignacionNoEvaluableException();
        }
    }

    public void CompletarEvaluacion(DateTimeOffset fechaModificacion, Guid usuarioEvaluadorId)
    {
        ValidarParametrosTransicion(fechaModificacion, usuarioEvaluadorId);

        if (usuarioEvaluadorId != UsuarioEvaluadorId)
        {
            throw new EvaluacionConfidencialException();
        }

        switch (Estado)
        {
            case EstadoAsignacionEvaluacion.EnProgreso:
                ActualizarAuditoria(usuarioEvaluadorId, fechaModificacion);
                Estado = EstadoAsignacionEvaluacion.Completada;
                return;

            case EstadoAsignacionEvaluacion.Completada:
                throw new EvaluacionYaEnviadaException();

            default:
                throw new AsignacionNoEvaluableException();
        }
    }

    private static void ValidarParametrosTransicion(
        DateTimeOffset fechaModificacion,
        Guid usuarioEvaluadorId)
    {
        if (fechaModificacion == default)
        {
            throw new ArgumentException(
                "La fecha de modificación es obligatoria.",
                nameof(fechaModificacion));
        }

        if (usuarioEvaluadorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario evaluador es obligatorio.",
                nameof(usuarioEvaluadorId));
        }
    }
}
