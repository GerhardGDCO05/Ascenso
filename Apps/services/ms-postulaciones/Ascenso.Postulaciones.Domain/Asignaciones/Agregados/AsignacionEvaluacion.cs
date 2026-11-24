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
}
