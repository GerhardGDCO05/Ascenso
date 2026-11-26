using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionEvaluacionesCriteriosTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionEvaluacionesCriterios_Cuando_SeMigraDesdeExcepcionEvaluadorUnico()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(ExcepcionEvaluadorUnico),
            toMigration: nameof(EvaluacionesCriterios));

        script.Should().Contain("CREATE TABLE postulaciones.evaluaciones");
        script.Should().Contain("pk_evaluaciones");
        script.Should().Contain("asignacion_evaluacion_id uuid NOT NULL");
        script.Should().Contain("postulacion_id uuid NOT NULL");
        script.Should().Contain("convocatoria_id uuid NOT NULL");
        script.Should().Contain("rubrica_evaluacion_id uuid NOT NULL");
        script.Should().Contain("evaluador_id uuid NOT NULL");
        script.Should().Contain("fecha_inicio timestamptz NOT NULL");
        script.Should().Contain("fecha_envio timestamptz");
        script.Should().Contain("puntaje_total_ponderado numeric(5,2)");
        script.Should().Contain("comentarios_generales character varying(2000)");
        script.Should().Contain("estado varchar NOT NULL");
        script.Should().Contain("motivo_invalidacion character varying(1000)");

        script.Should().Contain("fk_evaluaciones_asignaciones_evaluacion");
        script.Should().Contain("fk_evaluaciones_rubricas");
        script.Should().Contain("ON DELETE RESTRICT");

        script.Should().Contain("ck_evaluaciones_estado_valido");
        script.Should().Contain("'BORRADOR'");
        script.Should().Contain("'ENVIADA_DEFINITIVA'");
        script.Should().Contain("'INVALIDADA_POR_CONFLICTO'");
        script.Should().Contain("ck_evaluaciones_puntaje_total_rango");
        script.Should().Contain("ck_evaluaciones_envio_coherente");
        script.Should().Contain("ck_evaluaciones_comentarios_generales_no_vacios");

        script.Should().Contain("ux_evaluaciones_asignacion_evaluacion");
        script.Should().Contain("ix_evaluaciones_evaluador_estado");
        script.Should().Contain("ix_evaluaciones_postulacion_estado");

        script.Should().Contain("CREATE TABLE postulaciones.calificaciones_criterio");
        script.Should().Contain("pk_calificaciones_criterio");
        script.Should().Contain("evaluacion_id uuid NOT NULL");
        script.Should().Contain("criterio_evaluacion_id uuid NOT NULL");
        script.Should().Contain("puntaje_asignado numeric(7,2) NOT NULL");
        script.Should().Contain("comentario character varying(1000)");

        script.Should().Contain("fk_calificaciones_criterio_evaluaciones");
        script.Should().Contain("ON DELETE CASCADE");
        script.Should().Contain("fk_calificaciones_criterio_criterios_evaluacion");

        script.Should().Contain("ck_calificaciones_criterio_puntaje_rango");
        script.Should().Contain("ck_calificaciones_criterio_comentario_no_vacio");
        script.Should().Contain("ux_calificaciones_criterio_evaluacion_criterio");
    }
}
