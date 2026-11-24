using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionAsignacionesEvaluacionTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionAsignacionesEvaluacion_Cuando_SeMigraDesdeComitesEvaluadores()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(ComitesEvaluadores),
            toMigration: nameof(AsignacionesEvaluacion));

        script.Should().Contain("CREATE TABLE postulaciones.asignaciones_evaluacion");
        script.Should().Contain("pk_asignaciones_evaluacion");
        script.Should().Contain("comite_evaluador_id uuid NOT NULL");
        script.Should().Contain("convocatoria_id uuid NOT NULL");
        script.Should().Contain("postulacion_id uuid NOT NULL");
        script.Should().Contain("miembro_comite_id uuid NOT NULL");
        script.Should().Contain("usuario_evaluador_id uuid NOT NULL");
        script.Should().Contain("fecha_asignacion timestamptz NOT NULL");
        script.Should().Contain("fecha_limite_evaluacion timestamptz NOT NULL");
        script.Should().Contain("estado varchar NOT NULL");
        script.Should().Contain("fecha_creacion timestamptz NOT NULL");
        script.Should().Contain("fecha_ultima_modificacion timestamptz NOT NULL");

        script.Should().Contain("fk_asignaciones_comites");
        script.Should().Contain("fk_asignaciones_miembros_comite");
        script.Should().Contain("ON DELETE RESTRICT");

        script.Should().Contain("ck_asignaciones_estado_valido");
        script.Should().Contain("'PENDIENTE'");
        script.Should().Contain("'EN_PROGRESO'");
        script.Should().Contain("'COMPLETADA'");
        script.Should().Contain("'REASIGNADA_POR_CONFLICTO'");
        script.Should().Contain("ck_asignaciones_fecha_limite_posterior");
        script.Should().Contain("fecha_limite_evaluacion > fecha_asignacion");

        script.Should().Contain("ux_asignaciones_postulacion_evaluador_vigente");
        script.Should().Contain("WHERE estado <> 'REASIGNADA_POR_CONFLICTO'");
        script.Should().Contain("ix_asignaciones_evaluador_estado");
        script.Should().Contain("ix_asignaciones_comite_estado");
        script.Should().Contain("ix_asignaciones_postulacion");
    }
}
