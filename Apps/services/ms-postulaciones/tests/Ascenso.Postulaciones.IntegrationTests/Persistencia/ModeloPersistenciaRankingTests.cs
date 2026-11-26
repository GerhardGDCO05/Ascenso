using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaRankingTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_MapearColumnasIndicesParcialesYRestriccionesCheckDeConfirmacionRanking_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(ResultadoConsolidadoRanking));

        entidad.Should().NotBeNull();
        entidad!.FindProperty(nameof(ResultadoConsolidadoRanking.EsAdjudicable)).Should().BeNull();

        var posicionRanking = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.PosicionRanking))!;
        posicionRanking.Should().NotBeNull();
        posicionRanking.GetColumnName().Should().Be("posicion_ranking");
        posicionRanking.GetColumnType().Should().Be("integer");
        posicionRanking.IsNullable.Should().BeTrue();

        var montoEstimadoBeca = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.MontoEstimadoBeca))!;
        montoEstimadoBeca.Should().NotBeNull();
        montoEstimadoBeca.GetColumnName().Should().Be("monto_estimado_beca");
        montoEstimadoBeca.GetPrecision().Should().Be(12);
        montoEstimadoBeca.GetScale().Should().Be(2);
        montoEstimadoBeca.IsNullable.Should().BeTrue();

        var clasificacion = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.Clasificacion))!;
        clasificacion.Should().NotBeNull();
        clasificacion.GetColumnName().Should().Be("clasificacion");
        clasificacion.GetColumnType().Should().Be("varchar");
        clasificacion.GetMaxLength().Should().Be(20);
        clasificacion.IsNullable.Should().BeTrue();

        var convertidorClasificacion = clasificacion.GetValueConverter();
        convertidorClasificacion.Should().NotBeNull();
        convertidorClasificacion!.ConvertToProvider(ClasificacionRanking.DentroDeCupo).Should().Be("DENTRO_DE_CUPO");
        convertidorClasificacion.ConvertToProvider(ClasificacionRanking.ListaDeEspera).Should().Be("LISTA_DE_ESPERA");
        convertidorClasificacion.ConvertFromProvider("DENTRO_DE_CUPO").Should().Be(ClasificacionRanking.DentroDeCupo);
        convertidorClasificacion.ConvertFromProvider("LISTA_DE_ESPERA").Should().Be(ClasificacionRanking.ListaDeEspera);

        var fechaConfirmacionRanking = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.FechaConfirmacionRanking))!;
        fechaConfirmacionRanking.Should().NotBeNull();
        fechaConfirmacionRanking.GetColumnName().Should().Be("fecha_confirmacion_ranking");
        fechaConfirmacionRanking.GetColumnType().Should().Be("timestamptz");
        fechaConfirmacionRanking.IsNullable.Should().BeTrue();

        var confirmadoPorUsuarioId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.ConfirmadoPorUsuarioId))!;
        confirmadoPorUsuarioId.Should().NotBeNull();
        confirmadoPorUsuarioId.GetColumnName().Should().Be("confirmado_por_usuario_id");
        confirmadoPorUsuarioId.GetColumnType().Should().Be("uuid");
        confirmadoPorUsuarioId.IsNullable.Should().BeTrue();

        var indiceConvocatoriaPosicion = entidad.GetIndexes()
            .SingleOrDefault(indice => indice.GetDatabaseName() == "ux_resultados_consolidados_ranking_convocatoria_posicion");
        indiceConvocatoriaPosicion.Should().NotBeNull();
        indiceConvocatoriaPosicion!.IsUnique.Should().BeTrue();
        indiceConvocatoriaPosicion.GetFilter().Should().Be("posicion_ranking IS NOT NULL");
        indiceConvocatoriaPosicion.Properties.Select(p => p.GetColumnName())
            .Should().Equal("convocatoria_id", "posicion_ranking");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().Contain([
            "ck_resultados_consolidados_clasificacion_valida",
            "ck_resultados_consolidados_ranking_campos_rango",
            "ck_resultados_consolidados_confirmacion_coherente"
        ]);

        checks["ck_resultados_consolidados_clasificacion_valida"].Should().Be(
            "clasificacion IS NULL OR clasificacion IN ('DENTRO_DE_CUPO', 'LISTA_DE_ESPERA')");
        checks["ck_resultados_consolidados_ranking_campos_rango"].Should().Be(
            "(posicion_ranking IS NULL OR posicion_ranking >= 1) AND (monto_estimado_beca IS NULL OR monto_estimado_beca > 0)");
        checks["ck_resultados_consolidados_confirmacion_coherente"].Should().Be(
            "(estado_confirmacion_ranking = 'PROVISIONAL' AND posicion_ranking IS NULL AND monto_estimado_beca IS NULL AND clasificacion IS NULL AND fecha_confirmacion_ranking IS NULL AND confirmado_por_usuario_id IS NULL) OR (estado_confirmacion_ranking = 'CONFIRMADO' AND posicion_ranking IS NOT NULL AND monto_estimado_beca IS NOT NULL AND clasificacion IS NOT NULL AND fecha_confirmacion_ranking IS NOT NULL AND confirmado_por_usuario_id IS NOT NULL) OR estado_confirmacion_ranking NOT IN ('PROVISIONAL', 'CONFIRMADO')");
    }

    [Fact]
    public void Debe_IncluirColumnasIndiceParcialYRestriccionesCheckDeConfirmacionRanking_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("posicion_ranking integer");
        script.Should().Contain("monto_estimado_beca numeric(12,2)");
        script.Should().Contain("clasificacion varchar");
        script.Should().Contain("fecha_confirmacion_ranking timestamptz");
        script.Should().Contain("confirmado_por_usuario_id uuid");
        script.Should().Contain("ux_resultados_consolidados_ranking_convocatoria_posicion");
        script.Should().Contain("WHERE posicion_ranking IS NOT NULL");
        script.Should().Contain("ck_resultados_consolidados_clasificacion_valida");
        script.Should().Contain("ck_resultados_consolidados_ranking_campos_rango");
        script.Should().Contain("ck_resultados_consolidados_confirmacion_coherente");
    }
}
