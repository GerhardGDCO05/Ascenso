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
    public const string IndiceExcepcionEvaluadorUnicoPostulacionUnico = "ux_excepciones_evaluador_unico_postulacion";
    public const string IndiceEvaluacionAsignacionUnico = "ux_evaluaciones_asignacion_evaluacion";
    public const string IndiceResultadoConsolidadoPostulacionUnico = "ux_resultados_consolidados_ranking_postulacion";
    public const string IndiceResultadoConsolidadoConvocatoriaPosicionUnico = "ux_resultados_consolidados_ranking_convocatoria_posicion";
    public const string IndiceEntradaListaEsperaPostulacionUnico = "ux_entradas_lista_espera_postulacion";
    public const string IndiceEntradaListaEsperaConvocatoriaPosicionUnico = "ux_entradas_lista_espera_convocatoria_posicion";

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

        if (string.Equals(pg.ConstraintName, IndiceExcepcionEvaluadorUnicoPostulacionUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new ExcepcionEvaluadorUnicoYaAutorizadaException();
        }

        if (string.Equals(pg.ConstraintName, IndiceEvaluacionAsignacionUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new EvaluacionDuplicadaException();
        }

        if (string.Equals(pg.ConstraintName, IndiceResultadoConsolidadoPostulacionUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoConsolidadoDuplicadoException();
        }

        if (string.Equals(pg.ConstraintName, IndiceResultadoConsolidadoConvocatoriaPosicionUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new ExcepcionConflictoConcurrencia(
                "Ya existe un resultado confirmado con la misma posición de ranking en la convocatoria.",
                ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
        }

        if (string.Equals(pg.ConstraintName, IndiceEntradaListaEsperaPostulacionUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new EntradaListaEsperaDuplicadaException();
        }

        if (string.Equals(pg.ConstraintName, IndiceEntradaListaEsperaConvocatoriaPosicionUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new ExcepcionConflictoConcurrencia(
                "Ya existe una entrada en lista de espera con la misma posición de orden en la convocatoria.",
                ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
        }

        return new ExcepcionConflictoConcurrencia(
            "Ya existe un registro con los mismos datos únicos.",
            ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
    }
}
