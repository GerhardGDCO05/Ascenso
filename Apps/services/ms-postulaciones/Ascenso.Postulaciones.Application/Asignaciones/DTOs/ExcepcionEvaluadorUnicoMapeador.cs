using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;

namespace Ascenso.Postulaciones.Application.Asignaciones.DTOs;

public static class ExcepcionEvaluadorUnicoMapeador
{
    public static ExcepcionEvaluadorUnicoDto ADto(this AutorizacionExcepcionEvaluadorUnico autorizacion)
    {
        ArgumentNullException.ThrowIfNull(autorizacion);

        return new ExcepcionEvaluadorUnicoDto(
            autorizacion.Id,
            autorizacion.PostulacionId,
            autorizacion.Justificacion,
            autorizacion.UsuarioAutorId,
            autorizacion.FechaCreacion);
    }
}
