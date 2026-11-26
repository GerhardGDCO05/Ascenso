using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionRubricasEvaluacionTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracion_Cuando_SeMigraDesdeElPerfilSocioeconomico()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(PerfilSocioeconomicoGrupoFamiliar),
            toMigration: nameof(RubricasEvaluacion));

        script.Should().Contain("CREATE TABLE postulaciones.rubricas");
        script.Should().Contain("pk_rubricas");
        script.Should().Contain("ck_rubricas_estado_valido");
        script.Should().Contain("ux_rubricas_programa_convocatoria");
        script.Should().Contain("CREATE TABLE postulaciones.criterios_evaluacion");
        script.Should().Contain("pk_criterios_evaluacion");
        script.Should().Contain("fk_criterios_evaluacion_rubricas");
        script.Should().Contain("ON DELETE CASCADE");
        script.Should().Contain("ck_criterios_evaluacion_peso_rango");
        script.Should().Contain("ck_criterios_evaluacion_puntaje_rango");
        script.Should().Contain("ck_criterios_evaluacion_orden_positivo");
        script.Should().Contain("ux_criterios_evaluacion_rubrica_orden");
    }

    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionBloqueoRubrica_Cuando_SeMigraDesdeRubricasEvaluacion()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(RubricasEvaluacion),
            toMigration: nameof(BloqueoRubrica));

        script.Should().Contain("motivo_bloqueo");
        script.Should().Contain("fecha_bloqueo");
        script.Should().Contain("ck_rubricas_motivo_bloqueo_valido");
        script.Should().Contain("ck_rubricas_bloqueo_coherente");
    }
}
