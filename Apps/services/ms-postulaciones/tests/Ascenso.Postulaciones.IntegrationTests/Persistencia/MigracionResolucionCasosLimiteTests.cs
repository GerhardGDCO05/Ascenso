using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionResolucionCasosLimiteTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionResolucionCasosLimiteRanking_Cuando_SeMigraDesdeConfirmacionRankingConvocatoria()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(ConfirmacionRankingConvocatoria),
            toMigration: nameof(ResolucionCasosLimiteRanking));

        script.Should().Contain("ALTER TABLE postulaciones.resultados_consolidados_ranking");
        script.Should().Contain("decidido_por_usuario_id uuid");
        script.Should().Contain("fecha_decision_caso_limite timestamptz");
        script.Should().Contain("justificacion_desempate_caso_limite character varying(1000)");
        script.Should().Contain("seleccionado_en_caso_limite boolean");
        script.Should().Contain("tipo_caso_limite varchar");

        script.Should().Contain("ck_resultados_consolidados_tipo_caso_limite_valido");
        script.Should().Contain("'EMPATE_POR_CUPO'");
        script.Should().Contain("'EMPATE_POR_PRESUPUESTO'");
        script.Should().Contain("'CASO_LIMITE_PRESUPUESTARIO'");
        script.Should().Contain("ck_resultados_consolidados_caso_limite_coherente");
        script.Should().Contain("OR (tipo_caso_limite IS NOT NULL AND tipo_caso_limite NOT IN ('EMPATE_POR_CUPO', 'EMPATE_POR_PRESUPUESTO', 'CASO_LIMITE_PRESUPUESTARIO'))");
    }
}
