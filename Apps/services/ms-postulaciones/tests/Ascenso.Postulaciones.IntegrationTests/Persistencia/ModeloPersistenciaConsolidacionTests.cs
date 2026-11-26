using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaConsolidacionTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_MapearResultadoConsolidadoRankingConEsquemaColumnasPrecisionesIndicesYRestriccionesCheck_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(ResultadoConsolidadoRanking));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("resultados_consolidados_ranking");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_resultados_consolidados_ranking");

        var propiedadId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");
        propiedadId.GetColumnType().Should().Be("uuid");

        var propiedadVersion = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();
        propiedadVersion.GetColumnName().Should().Be("version");

        var fechaCreacion = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.FechaCreacion))!;
        fechaCreacion.GetColumnName().Should().Be("fecha_creacion");
        fechaCreacion.GetColumnType().Should().Be("timestamptz");

        var usuarioCreacionId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.UsuarioCreacionId))!;
        usuarioCreacionId.GetColumnName().Should().Be("usuario_creacion_id");
        usuarioCreacionId.GetColumnType().Should().Be("uuid");

        var fechaModificacion = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.FechaUltimaModificacion))!;
        fechaModificacion.GetColumnName().Should().Be("fecha_ultima_modificacion");
        fechaModificacion.GetColumnType().Should().Be("timestamptz");

        var usuarioModificacionId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.UsuarioModificacionId))!;
        usuarioModificacionId.GetColumnName().Should().Be("usuario_modificacion_id");
        usuarioModificacionId.GetColumnType().Should().Be("uuid");

        var convocatoriaId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.ConvocatoriaId))!;
        convocatoriaId.GetColumnName().Should().Be("convocatoria_id");
        convocatoriaId.GetColumnType().Should().Be("uuid");
        convocatoriaId.IsNullable.Should().BeFalse();

        var postulacionId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.PostulacionId))!;
        postulacionId.GetColumnName().Should().Be("postulacion_id");
        postulacionId.GetColumnType().Should().Be("uuid");
        postulacionId.IsNullable.Should().BeFalse();

        var excepcionEvaluadorUnico = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.ExcepcionEvaluadorUnico))!;
        excepcionEvaluadorUnico.GetColumnName().Should().Be("excepcion_evaluador_unico");
        excepcionEvaluadorUnico.GetColumnType().Should().Be("boolean");
        excepcionEvaluadorUnico.IsNullable.Should().BeFalse();

        var justificacionEvaluadorUnico = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.JustificacionEvaluadorUnico))!;
        justificacionEvaluadorUnico.GetColumnName().Should().Be("justificacion_evaluador_unico");
        justificacionEvaluadorUnico.GetMaxLength().Should().Be(ResultadoConsolidadoRanking.LongitudMaximaJustificacionExcepcion);
        justificacionEvaluadorUnico.IsNullable.Should().BeTrue();

        var autorizadorExcepcionId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.AutorizadorExcepcionId))!;
        autorizadorExcepcionId.GetColumnName().Should().Be("autorizador_excepcion_id");
        autorizadorExcepcionId.GetColumnType().Should().Be("uuid");
        autorizadorExcepcionId.IsNullable.Should().BeTrue();

        var cantidadEvaluacionesValidas = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.CantidadEvaluacionesValidas))!;
        cantidadEvaluacionesValidas.GetColumnName().Should().Be("cantidad_evaluaciones_validas");
        cantidadEvaluacionesValidas.GetColumnType().Should().Be("integer");
        cantidadEvaluacionesValidas.IsNullable.Should().BeFalse();

        var puntajeConsolidado = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.PuntajeConsolidado))!;
        puntajeConsolidado.GetColumnName().Should().Be("puntaje_consolidado");
        puntajeConsolidado.GetPrecision().Should().Be(5);
        puntajeConsolidado.GetScale().Should().Be(2);
        puntajeConsolidado.IsNullable.Should().BeFalse();

        var diferenciaEntreEvaluadores = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.DiferenciaEntreEvaluadores))!;
        diferenciaEntreEvaluadores.GetColumnName().Should().Be("diferencia_entre_evaluadores");
        diferenciaEntreEvaluadores.GetPrecision().Should().Be(5);
        diferenciaEntreEvaluadores.GetScale().Should().Be(2);
        diferenciaEntreEvaluadores.IsNullable.Should().BeFalse();

        var umbralDiscrepanciaAplicado = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.UmbralDiscrepanciaAplicado))!;
        umbralDiscrepanciaAplicado.GetColumnName().Should().Be("umbral_discrepancia_aplicado");
        umbralDiscrepanciaAplicado.GetPrecision().Should().Be(5);
        umbralDiscrepanciaAplicado.GetScale().Should().Be(2);
        umbralDiscrepanciaAplicado.IsNullable.Should().BeFalse();

        var alertaDiscrepancia = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.AlertaDiscrepancia))!;
        alertaDiscrepancia.GetColumnName().Should().Be("alerta_discrepancia");
        alertaDiscrepancia.GetColumnType().Should().Be("boolean");
        alertaDiscrepancia.IsNullable.Should().BeFalse();

        var discrepanciaRevisada = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.DiscrepanciaRevisada))!;
        discrepanciaRevisada.GetColumnName().Should().Be("discrepancia_revisada");
        discrepanciaRevisada.GetColumnType().Should().Be("boolean");
        discrepanciaRevisada.IsNullable.Should().BeFalse();

        var comentarioRevisionDiscrepancia = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.ComentarioRevisionDiscrepancia))!;
        comentarioRevisionDiscrepancia.GetColumnName().Should().Be("comentario_revision_discrepancia");
        comentarioRevisionDiscrepancia.GetMaxLength().Should().Be(ResultadoConsolidadoRanking.LongitudMaximaComentarioRevision);
        comentarioRevisionDiscrepancia.IsNullable.Should().BeTrue();

        var revisadoPorUsuarioId = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.RevisadoPorUsuarioId))!;
        revisadoPorUsuarioId.GetColumnName().Should().Be("revisado_por_usuario_id");
        revisadoPorUsuarioId.GetColumnType().Should().Be("uuid");
        revisadoPorUsuarioId.IsNullable.Should().BeTrue();

        var fechaRevisionDiscrepancia = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.FechaRevisionDiscrepancia))!;
        fechaRevisionDiscrepancia.GetColumnName().Should().Be("fecha_revision_discrepancia");
        fechaRevisionDiscrepancia.GetColumnType().Should().Be("timestamptz");
        fechaRevisionDiscrepancia.IsNullable.Should().BeTrue();

        var estadoConfirmacion = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.EstadoConfirmacionRanking))!;
        estadoConfirmacion.GetColumnName().Should().Be("estado_confirmacion_ranking");
        estadoConfirmacion.GetColumnType().Should().Be("varchar");
        estadoConfirmacion.GetMaxLength().Should().Be(20);
        estadoConfirmacion.IsNullable.Should().BeFalse();

        var convertidorEstado = estadoConfirmacion.GetValueConverter();
        convertidorEstado.Should().NotBeNull();
        convertidorEstado!.ConvertToProvider(EstadoRanking.Provisional).Should().Be("PROVISIONAL");
        convertidorEstado.ConvertToProvider(EstadoRanking.Confirmado).Should().Be("CONFIRMADO");
        convertidorEstado.ConvertFromProvider("PROVISIONAL").Should().Be(EstadoRanking.Provisional);
        convertidorEstado.ConvertFromProvider("CONFIRMADO").Should().Be(EstadoRanking.Confirmado);

        var fechaConsolidacion = entidad.FindProperty(nameof(ResultadoConsolidadoRanking.FechaConsolidacion))!;
        fechaConsolidacion.GetColumnName().Should().Be("fecha_consolidacion");
        fechaConsolidacion.GetColumnType().Should().Be("timestamptz");
        fechaConsolidacion.IsNullable.Should().BeFalse();

        var indicePostulacionUnico = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_resultados_consolidados_ranking_postulacion");
        indicePostulacionUnico.IsUnique.Should().BeTrue();
        indicePostulacionUnico.Properties.Select(p => p.GetColumnName())
            .Should().Equal("postulacion_id");

        var indiceConvocatoriaPuntaje = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ix_resultados_consolidados_ranking_convocatoria_puntaje");
        indiceConvocatoriaPuntaje.IsUnique.Should().BeFalse();
        indiceConvocatoriaPuntaje.Properties.Select(p => p.GetColumnName())
            .Should().Equal("convocatoria_id", "puntaje_consolidado", "fecha_consolidacion", "id");

        var indiceConvocatoriaDiscrepancia = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ix_resultados_consolidados_ranking_convocatoria_discrepancia");
        indiceConvocatoriaDiscrepancia.IsUnique.Should().BeFalse();
        indiceConvocatoriaDiscrepancia.Properties.Select(p => p.GetColumnName())
            .Should().Equal("convocatoria_id", "alerta_discrepancia", "discrepancia_revisada");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().Contain([
            "ck_resultados_consolidados_estado_valido",
            "ck_resultados_consolidados_puntaje_rango",
            "ck_resultados_consolidados_diferencia_rango",
            "ck_resultados_consolidados_umbral_rango",
            "ck_resultados_consolidados_excepcion_coherente",
            "ck_resultados_consolidados_alerta_coherente",
            "ck_resultados_consolidados_revision_coherente"
        ]);

        checks["ck_resultados_consolidados_estado_valido"].Should().Contain("'PROVISIONAL'");
        checks["ck_resultados_consolidados_estado_valido"].Should().Contain("'CONFIRMADO'");
        checks["ck_resultados_consolidados_puntaje_rango"].Should().Be(
            "puntaje_consolidado >= 0 AND puntaje_consolidado <= 100");
        checks["ck_resultados_consolidados_diferencia_rango"].Should().Be(
            "diferencia_entre_evaluadores >= 0 AND diferencia_entre_evaluadores <= 100");
        checks["ck_resultados_consolidados_umbral_rango"].Should().Be(
            "umbral_discrepancia_aplicado > 0 AND umbral_discrepancia_aplicado <= 100");
        checks["ck_resultados_consolidados_excepcion_coherente"].Should().Be(
            "(excepcion_evaluador_unico = true AND cantidad_evaluaciones_validas = 1 AND justificacion_evaluador_unico IS NOT NULL AND length(btrim(justificacion_evaluador_unico)) > 0 AND autorizador_excepcion_id IS NOT NULL AND diferencia_entre_evaluadores = 0 AND alerta_discrepancia = false) OR (excepcion_evaluador_unico = false AND cantidad_evaluaciones_validas >= 2 AND justificacion_evaluador_unico IS NULL AND autorizador_excepcion_id IS NULL)");
        checks["ck_resultados_consolidados_alerta_coherente"].Should().Be(
            "(excepcion_evaluador_unico = true AND alerta_discrepancia = false) OR (excepcion_evaluador_unico = false AND alerta_discrepancia = (diferencia_entre_evaluadores > umbral_discrepancia_aplicado))");
        checks["ck_resultados_consolidados_revision_coherente"].Should().Be(
            "(discrepancia_revisada = false AND comentario_revision_discrepancia IS NULL AND revisado_por_usuario_id IS NULL AND fecha_revision_discrepancia IS NULL) OR (discrepancia_revisada = true AND alerta_discrepancia = true AND comentario_revision_discrepancia IS NOT NULL AND length(btrim(comentario_revision_discrepancia)) > 0 AND revisado_por_usuario_id IS NOT NULL AND fecha_revision_discrepancia IS NOT NULL)");

        contexto.ResultadosConsolidadosRanking.Should().NotBeNull();
    }

    [Fact]
    public void Debe_IncluirTablaResultadosConsolidadosRankingConRestriccionesEIndices_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE postulaciones.resultados_consolidados_ranking");
        script.Should().Contain("pk_resultados_consolidados_ranking");
        script.Should().Contain("ux_resultados_consolidados_ranking_postulacion");
        script.Should().Contain("ix_resultados_consolidados_ranking_convocatoria_puntaje");
        script.Should().Contain("ix_resultados_consolidados_ranking_convocatoria_discrepancia");
        script.Should().Contain("ck_resultados_consolidados_estado_valido");
        script.Should().Contain("ck_resultados_consolidados_puntaje_rango");
        script.Should().Contain("ck_resultados_consolidados_diferencia_rango");
        script.Should().Contain("ck_resultados_consolidados_umbral_rango");
        script.Should().Contain("ck_resultados_consolidados_excepcion_coherente");
        script.Should().Contain("ck_resultados_consolidados_alerta_coherente");
        script.Should().Contain("ck_resultados_consolidados_revision_coherente");
    }
}
