using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;

/// <summary>
/// Entidad hija del agregado Evaluación que representa el puntaje y comentario asignado
/// a un criterio específico de la rúbrica.
/// </summary>
public sealed class CalificacionCriterio : EntidadAuditable
{
    // Constructor requerido por los adaptadores de persistencia.
    private CalificacionCriterio()
    {
    }

    private CalificacionCriterio(
        Guid evaluacionId,
        Guid criterioEvaluacionId,
        decimal puntajeAsignado,
        string? comentario,
        DateTimeOffset fechaCreacion,
        Guid usuarioEvaluadorId)
        : base(UuidV7.Nuevo(), fechaCreacion, usuarioEvaluadorId)
    {
        EvaluacionId = evaluacionId;
        CriterioEvaluacionId = criterioEvaluacionId;
        PuntajeAsignado = puntajeAsignado;
        Comentario = comentario;
    }

    public Guid EvaluacionId { get; private set; }

    public Guid CriterioEvaluacionId { get; private set; }

    public decimal PuntajeAsignado { get; private set; }

    public string? Comentario { get; private set; }

    internal static CalificacionCriterio Crear(
        Guid evaluacionId,
        CriterioEvaluacion criterio,
        DatosCalificacionCriterio datos,
        DateTimeOffset fechaCreacion,
        Guid usuarioEvaluadorId)
    {
        if (evaluacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la evaluación es obligatorio.",
                nameof(evaluacionId));
        }

        ArgumentNullException.ThrowIfNull(criterio);
        ArgumentNullException.ThrowIfNull(datos);

        if (criterio.Id != datos.CriterioEvaluacionId)
        {
            throw new CriterioNoPerteneceARubricaException();
        }

        ValidarPuntajeEnRango(criterio, datos.PuntajeAsignado);

        return new CalificacionCriterio(
            evaluacionId,
            criterio.Id,
            datos.PuntajeAsignado,
            datos.Comentario,
            fechaCreacion,
            usuarioEvaluadorId);
    }

    internal void Actualizar(
        CriterioEvaluacion criterio,
        DatosCalificacionCriterio datos,
        DateTimeOffset fechaModificacion,
        Guid usuarioEvaluadorId)
    {
        ArgumentNullException.ThrowIfNull(criterio);
        ArgumentNullException.ThrowIfNull(datos);

        if (criterio.Id != CriterioEvaluacionId || datos.CriterioEvaluacionId != CriterioEvaluacionId)
        {
            throw new CriterioNoPerteneceARubricaException();
        }

        ValidarPuntajeEnRango(criterio, datos.PuntajeAsignado);

        ActualizarAuditoria(usuarioEvaluadorId, fechaModificacion);
        PuntajeAsignado = datos.PuntajeAsignado;
        Comentario = datos.Comentario;
    }

    private static void ValidarPuntajeEnRango(CriterioEvaluacion criterio, decimal puntajeAsignado)
    {
        if (puntajeAsignado < criterio.PuntajeMinimo || puntajeAsignado > criterio.PuntajeMaximo)
        {
            throw new PuntajeFueraDeRangoException(
                criterio.Nombre,
                criterio.PuntajeMinimo,
                criterio.PuntajeMaximo,
                puntajeAsignado);
        }
    }
}
