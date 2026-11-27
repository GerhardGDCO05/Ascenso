using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaCasoLimiteTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_MapearColumnasYRestriccionesCheckDeCasoLimiteSinAlterarRestriccionesPrevias_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(ResultadoConsolidadoRanking));

        entidad.Should().NotBeNull();
        entidad!.FindProperty(nameof(ResultadoConsolidadoRanking.EsAdjudicable)).Should().BeNull();
        entidad.FindProperty(nameof(ResultadoConsolidadoRanking.TieneDecisionCasoLimite)).Should().BeNull();

        var tipoCasoLimite = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.TipoCasoLimite))!;
        tipoCasoLimite.Should().NotBeNull();
        tipoCasoLimite.GetColumnName().Should().Be("tipo_caso_limite");
        tipoCasoLimite.GetColumnType().Should().Be("varchar");
        tipoCasoLimite.GetMaxLength().Should().Be(40);
        tipoCasoLimite.IsNullable.Should().BeTrue();

        var convertidorTipo = tipoCasoLimite.GetValueConverter();
        convertidorTipo.Should().NotBeNull();
        convertidorTipo!.ConvertToProvider(TipoCasoLimite.EmpatePorCupo).Should().Be("EMPATE_POR_CUPO");
        convertidorTipo.ConvertToProvider(TipoCasoLimite.EmpatePorPresupuesto).Should().Be("EMPATE_POR_PRESUPUESTO");
        convertidorTipo.ConvertToProvider(TipoCasoLimite.CasoLimitePresupuestario).Should().Be("CASO_LIMITE_PRESUPUESTARIO");
        convertidorTipo.ConvertFromProvider("EMPATE_POR_CUPO").Should().Be(TipoCasoLimite.EmpatePorCupo);
        convertidorTipo.ConvertFromProvider("EMPATE_POR_PRESUPUESTO").Should().Be(TipoCasoLimite.EmpatePorPresupuesto);
        convertidorTipo.ConvertFromProvider("CASO_LIMITE_PRESUPUESTARIO").Should().Be(TipoCasoLimite.CasoLimitePresupuestario);

        var seleccionado = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.SeleccionadoEnCasoLimite))!;
        seleccionado.Should().NotBeNull();
        seleccionado.GetColumnName().Should().Be("seleccionado_en_caso_limite");
        seleccionado.GetColumnType().Should().Be("boolean");
        seleccionado.IsNullable.Should().BeTrue();

        var justificacion = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.JustificacionDesempateCasoLimite))!;
        justificacion.Should().NotBeNull();
        justificacion.GetColumnName().Should().Be("justificacion_desempate_caso_limite");
        justificacion.GetMaxLength().Should().Be(ResultadoConsolidadoRanking.LongitudMaximaJustificacionCasoLimite);
        justificacion.IsNullable.Should().BeTrue();

        var decididoPor = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.DecididoPorUsuarioId))!;
        decididoPor.Should().NotBeNull();
        decididoPor.GetColumnName().Should().Be("decidido_por_usuario_id");
        decididoPor.GetColumnType().Should().Be("uuid");
        decididoPor.IsNullable.Should().BeTrue();

        var fechaDecision = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.FechaDecisionCasoLimite))!;
        fechaDecision.Should().NotBeNull();
        fechaDecision.GetColumnName().Should().Be("fecha_decision_caso_limite");
        fechaDecision.GetColumnType().Should().Be("timestamptz");
        fechaDecision.IsNullable.Should().BeTrue();

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);

        checks.Should().HaveCount(12);
        checks.Keys.Should().Contain([
            "ck_resultados_consolidados_estado_valido",
            "ck_resultados_consolidados_puntaje_rango",
            "ck_resultados_consolidados_diferencia_rango",
            "ck_resultados_consolidados_umbral_rango",
            "ck_resultados_consolidados_excepcion_coherente",
            "ck_resultados_consolidados_alerta_coherente",
            "ck_resultados_consolidados_revision_coherente",
            "ck_resultados_consolidados_clasificacion_valida",
            "ck_resultados_consolidados_ranking_campos_rango",
            "ck_resultados_consolidados_confirmacion_coherente",
            "ck_resultados_consolidados_tipo_caso_limite_valido",
            "ck_resultados_consolidados_caso_limite_coherente"
        ]);

        checks["ck_resultados_consolidados_tipo_caso_limite_valido"].Should().Be(
            "tipo_caso_limite IS NULL OR tipo_caso_limite IN ('EMPATE_POR_CUPO', 'EMPATE_POR_PRESUPUESTO', 'CASO_LIMITE_PRESUPUESTARIO')");
        checks["ck_resultados_consolidados_caso_limite_coherente"].Should().Be(
            "(tipo_caso_limite IS NULL AND seleccionado_en_caso_limite IS NULL AND justificacion_desempate_caso_limite IS NULL AND decidido_por_usuario_id IS NULL AND fecha_decision_caso_limite IS NULL) OR (tipo_caso_limite IS NOT NULL AND seleccionado_en_caso_limite IS NOT NULL AND justificacion_desempate_caso_limite IS NOT NULL AND length(btrim(justificacion_desempate_caso_limite)) >= 10 AND decidido_por_usuario_id IS NOT NULL AND fecha_decision_caso_limite IS NOT NULL) OR (tipo_caso_limite IS NOT NULL AND tipo_caso_limite NOT IN ('EMPATE_POR_CUPO', 'EMPATE_POR_PRESUPUESTO', 'CASO_LIMITE_PRESUPUESTARIO'))");
    }

    [Fact]
    public void Debe_IncluirColumnasYRestriccionesCheckDeCasoLimite_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("tipo_caso_limite varchar");
        script.Should().Contain("seleccionado_en_caso_limite boolean");
        script.Should().Contain("justificacion_desempate_caso_limite character varying(1000)");
        script.Should().Contain("decidido_por_usuario_id uuid");
        script.Should().Contain("fecha_decision_caso_limite timestamptz");
        script.Should().Contain("ck_resultados_consolidados_tipo_caso_limite_valido");
        script.Should().Contain("ck_resultados_consolidados_caso_limite_coherente");
    }
}
