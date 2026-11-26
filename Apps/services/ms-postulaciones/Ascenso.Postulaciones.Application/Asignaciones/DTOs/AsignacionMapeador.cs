using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

public static class AsignacionMapeador
{
    public static AsignacionEvaluacionDto ADto(this AsignacionEvaluacion asignacion)
    {
        ArgumentNullException.ThrowIfNull(asignacion);

        return new AsignacionEvaluacionDto(
            asignacion.Id,
            asignacion.ComiteEvaluadorId,
            asignacion.ConvocatoriaId,
            asignacion.PostulacionId,
            asignacion.MiembroComiteId,
            asignacion.UsuarioEvaluadorId,
            asignacion.Estado.ACodigo(),
            asignacion.FechaAsignacion,
            asignacion.FechaLimiteEvaluacion,
            asignacion.Version);
    }

    public static MiAsignacionDto AMiAsignacionDto(this AsignacionEvaluacion asignacion)
    {
        ArgumentNullException.ThrowIfNull(asignacion);

        return new MiAsignacionDto(
            asignacion.Id,
            asignacion.PostulacionId,
            asignacion.ConvocatoriaId,
            asignacion.Estado.ACodigo(),
            asignacion.FechaAsignacion,
            asignacion.FechaLimiteEvaluacion);
    }

    public static CargaEvaluadorDto ACargaDto(this MiembroComite miembro, int asignacionesAbiertas)
    {
        ArgumentNullException.ThrowIfNull(miembro);

        return new CargaEvaluadorDto(
            miembro.Id,
            miembro.UsuarioEvaluadorId,
            miembro.Tipo.ACodigo(),
            miembro.Rol.ACodigo(),
            asignacionesAbiertas);
    }
}
