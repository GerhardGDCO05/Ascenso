using Ascenso.Postulaciones.Domain.Excepciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Postulaciones.Infrastructure.Persistence;

/// <summary>
/// Traduce violaciones de restricciones únicas de PostgreSQL a excepciones de dominio.
/// </summary>
internal static class TraductorExcepcionesPersistencia
{
    public const string IndiceDocumentoUnico = "ux_postulante_numero_documento_identidad";
    public const string IndiceUsuarioUnico = "ux_postulante_usuario_id";
    public const string IndiceRubricaProgramaConvocatoriaUnico = "ux_rubricas_programa_convocatoria";
    public const string IndiceCriterioRubricaOrdenUnico = "ux_criterios_evaluacion_rubrica_orden";
    public const string IndiceComiteProgramaConvocatoriaUnico = "ux_comites_programa_convocatoria";
    public const string IndiceMiembroComiteUsuarioActivoUnico = "ux_miembros_comite_usuario_activo";
    public const string IndiceAsignacionPostulacionEvaluadorVigenteUnico = "ux_asignaciones_postulacion_evaluador_vigente";

    public static Exception? TraducirSiAplica(DbUpdateException excepcion)
    {
        ArgumentNullException.ThrowIfNull(excepcion);

        if (excepcion.InnerException is not PostgresException pg
            || !string.Equals(pg.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.Equals(pg.ConstraintName, IndiceDocumentoUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new IdentidadDuplicadaException();
        }

        if (string.Equals(pg.ConstraintName, IndiceUsuarioUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new PerfilYaRegistradoException();
        }

        if (string.Equals(pg.ConstraintName, IndiceRubricaProgramaConvocatoriaUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new RubricaDuplicadaException();
        }

        if (string.Equals(pg.ConstraintName, IndiceComiteProgramaConvocatoriaUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new ComiteDuplicadoException();
        }

        if (string.Equals(pg.ConstraintName, IndiceMiembroComiteUsuarioActivoUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new MiembroComiteDuplicadoException();
        }

        if (string.Equals(pg.ConstraintName, IndiceAsignacionPostulacionEvaluadorVigenteUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new AsignacionDuplicadaException();
        }

        return new ExcepcionConflictoConcurrencia(
            "Ya existe un registro con los mismos datos únicos.",
            ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
    }
}
