using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Asignaciones.Servicios;

/// <summary>
/// Servicio de dominio puro para autorizar una excepción de evaluador único
/// sobre una postulación (RN-POS-04).
/// </summary>
public static class ServicioExcepcionEvaluadorUnico
{
    public static AutorizacionExcepcionEvaluadorUnico Autorizar(
        Guid postulacionId,
        string justificacion,
        IReadOnlyCollection<AsignacionEvaluacion> vigentes,
        bool yaAutorizada,
        DateTimeOffset ahora,
        Guid usuarioAutorizadorId,
        Guid correlacionId)
    {
        ArgumentNullException.ThrowIfNull(vigentes);

        if (yaAutorizada)
        {
            throw new ExcepcionEvaluadorUnicoYaAutorizadaException();
        }

        var asignacionesVigentes = vigentes
            .Where(asignacion => asignacion.Estado != EstadoAsignacionEvaluacion.ReasignadaPorConflicto)
            .ToList();

        if (postulacionId != Guid.Empty
            && asignacionesVigentes.Any(asignacion => asignacion.PostulacionId != postulacionId))
        {
            throw new AsignacionEvaluacionInvalidaException(
                "Las asignaciones vigentes proporcionadas no corresponden a la postulación indicada.");
        }

        if (asignacionesVigentes.Count >= ServicioAsignacionEvaluadores.MinimoEvaluadoresVigentes)
        {
            throw new ExcepcionEvaluadorUnicoNoAplicableException();
        }

        return AutorizacionExcepcionEvaluadorUnico.Crear(
            postulacionId,
            justificacion,
            ahora,
            usuarioAutorizadorId,
            correlacionId);
    }
}
