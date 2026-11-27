using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionResultadosConsolidadosRankingTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionResultadosConsolidadosRanking_Cuando_SeMigraDesdeEvaluacionesCriterios()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(EvaluacionesCriterios),
            toMigration: nameof(ResultadosConsolidadosRanking));

        script.Should().Contain("CREATE TABLE postulaciones.resultados_consolidados_ranking");
        script.Should().Contain("pk_resultados_consolidados_ranking");
        script.Should().Contain("convocatoria_id uuid NOT NULL");
        script.Should().Contain("postulacion_id uuid NOT NULL");
        script.Should().Contain("excepcion_evaluador_unico boolean NOT NULL");
        script.Should().Contain("justificacion_evaluador_unico character varying(1000)");
        script.Should().Contain("autorizador_excepcion_id uuid");
        script.Should().Contain("cantidad_evaluaciones_validas integer NOT NULL");
        script.Should().Contain("puntaje_consolidado numeric(5,2) NOT NULL");
        script.Should().Contain("diferencia_entre_evaluadores numeric(5,2) NOT NULL");
        script.Should().Contain("umbral_discrepancia_aplicado numeric(5,2) NOT NULL");
        script.Should().Contain("alerta_discrepancia boolean NOT NULL");
        script.Should().Contain("discrepancia_revisada boolean NOT NULL");
        script.Should().Contain("comentario_revision_discrepancia character varying(1000)");
        script.Should().Contain("revisado_por_usuario_id uuid");
        script.Should().Contain("fecha_revision_discrepancia timestamptz");
        script.Should().Contain("estado_confirmacion_ranking varchar NOT NULL");
        script.Should().Contain("fecha_consolidacion timestamptz NOT NULL");

        script.Should().Contain("ck_resultados_consolidados_estado_valido");
        script.Should().Contain("'PROVISIONAL'");
        script.Should().Contain("'CONFIRMADO'");
        script.Should().Contain("ck_resultados_consolidados_puntaje_rango");
        script.Should().Contain("ck_resultados_consolidados_diferencia_rango");
        script.Should().Contain("ck_resultados_consolidados_umbral_rango");
        script.Should().Contain("ck_resultados_consolidados_excepcion_coherente");
        script.Should().Contain("ck_resultados_consolidados_alerta_coherente");
        script.Should().Contain("ck_resultados_consolidados_revision_coherente");

        script.Should().Contain("ux_resultados_consolidados_ranking_postulacion");
        script.Should().Contain("ix_resultados_consolidados_ranking_convocatoria_puntaje");
        script.Should().Contain("ix_resultados_consolidados_ranking_convocatoria_discrepancia");
    }
}
