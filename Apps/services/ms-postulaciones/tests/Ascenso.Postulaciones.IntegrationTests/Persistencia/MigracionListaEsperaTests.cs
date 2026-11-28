using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionListaEsperaTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionListaEsperaPostulaciones_Cuando_SeMigraDesdeResolucionCasosLimiteRanking()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(ResolucionCasosLimiteRanking),
            toMigration: nameof(ListaEsperaPostulaciones));

        script.Should().Contain("CREATE TABLE postulaciones.entradas_lista_espera");
        script.Should().NotContain("ALTER TABLE postulaciones.resultados_consolidados_ranking");

        script.Should().Contain("id uuid NOT NULL");
        script.Should().Contain("convocatoria_id uuid NOT NULL");
        script.Should().Contain("programa_beca_id uuid NOT NULL");
        script.Should().Contain("postulacion_id uuid NOT NULL");
        script.Should().Contain("posicion_orden integer NOT NULL");
        script.Should().Contain("monto_estimado_beca_snapshot numeric(12,2) NOT NULL");
        script.Should().Contain("estado varchar NOT NULL");
        script.Should().Contain("fecha_ingreso timestamptz NOT NULL");
        script.Should().Contain("fecha_elegibilidad timestamptz");
        script.Should().Contain("fecha_promocion timestamptz");
        script.Should().Contain("promovido_por_usuario_id uuid");
        script.Should().Contain("motivo_promocion character varying(500)");
        script.Should().Contain("version integer NOT NULL");

        script.Should().Contain("ux_entradas_lista_espera_postulacion");
        script.Should().Contain("ux_entradas_lista_espera_convocatoria_posicion");
        script.Should().Contain("ix_entradas_lista_espera_convocatoria_estado_posicion");

        script.Should().Contain("ck_entradas_lista_espera_estado_valido");
        script.Should().Contain("'EN_ESPERA'");
        script.Should().Contain("'ELEGIBLE_POR_VACANTE'");
        script.Should().Contain("'PROMOVIDA'");
        script.Should().Contain("ck_entradas_lista_espera_posicion_positiva");
        script.Should().Contain("ck_entradas_lista_espera_monto_positivo");
        script.Should().Contain("ck_entradas_lista_espera_estado_coherente");
        script.Should().Contain("OR estado NOT IN ('EN_ESPERA', 'ELEGIBLE_POR_VACANTE', 'PROMOVIDA')");
    }
}
