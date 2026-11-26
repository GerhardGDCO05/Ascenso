using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionConfirmacionRankingConvocatoriaTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionConfirmacionRankingConvocatoria_Cuando_SeMigraDesdeResultadosConsolidadosRanking()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(ResultadosConsolidadosRanking),
            toMigration: nameof(ConfirmacionRankingConvocatoria));

        script.Should().Contain("ALTER TABLE postulaciones.resultados_consolidados_ranking");
        script.Should().Contain("posicion_ranking integer");
        script.Should().Contain("monto_estimado_beca numeric(12,2)");
        script.Should().Contain("clasificacion varchar");
        script.Should().Contain("fecha_confirmacion_ranking timestamptz");
        script.Should().Contain("confirmado_por_usuario_id uuid");

        script.Should().Contain("ck_resultados_consolidados_clasificacion_valida");
        script.Should().Contain("'DENTRO_DE_CUPO'");
        script.Should().Contain("'LISTA_DE_ESPERA'");
        script.Should().Contain("ck_resultados_consolidados_ranking_campos_rango");
        script.Should().Contain("ck_resultados_consolidados_confirmacion_coherente");

        script.Should().Contain("ux_resultados_consolidados_ranking_convocatoria_posicion");
        script.Should().Contain("WHERE posicion_ranking IS NOT NULL");
    }
}
