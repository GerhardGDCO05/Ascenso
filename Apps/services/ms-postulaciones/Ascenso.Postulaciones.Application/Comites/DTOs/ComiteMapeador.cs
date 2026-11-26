using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Comites.DTOs;

public static class ComiteMapeador
{
    public static ComiteEvaluadorDto ADto(this ComiteEvaluador comite)
    {
        ArgumentNullException.ThrowIfNull(comite);

        return new ComiteEvaluadorDto(
            comite.Id,
            comite.ProgramaBecaId,
            comite.ConvocatoriaId,
            comite.Nombre,
            comite.JefaturaUsuarioId,
            comite.FechaConformacion,
            comite.ConformadoPorUsuarioId,
            comite.Activo,
            comite.Version,
            comite.Miembros
                .OrderBy(miembro => miembro.FechaAsignacion)
                .ThenBy(miembro => miembro.Id)
                .Select(miembro => miembro.ADto())
                .ToArray());
    }

    public static MiembroComiteDto ADto(this MiembroComite miembro)
    {
        ArgumentNullException.ThrowIfNull(miembro);

        return new MiembroComiteDto(
            miembro.Id,
            miembro.UsuarioEvaluadorId,
            miembro.Tipo.ACodigo(),
            miembro.Rol.ACodigo(),
            miembro.FechaAsignacion,
            miembro.FechaDesvinculacion,
            miembro.Activo);
    }
}
