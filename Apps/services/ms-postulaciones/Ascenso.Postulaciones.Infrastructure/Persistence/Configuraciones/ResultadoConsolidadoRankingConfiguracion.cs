using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el agregado <see cref="ResultadoConsolidadoRanking"/>.
/// </summary>
internal sealed class ResultadoConsolidadoRankingConfiguracion : IEntityTypeConfiguration<ResultadoConsolidadoRanking>
{
    public void Configure(EntityTypeBuilder<ResultadoConsolidadoRanking> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("resultados_consolidados_ranking", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_estado_valido",
                $"estado_confirmacion_ranking IN ({ConfiguracionPersistencia.ValoresSql<EstadoRanking>()})");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_puntaje_rango",
                "puntaje_consolidado >= 0 AND puntaje_consolidado <= 100");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_diferencia_rango",
                "diferencia_entre_evaluadores >= 0 AND diferencia_entre_evaluadores <= 100");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_umbral_rango",
                "umbral_discrepancia_aplicado > 0 AND umbral_discrepancia_aplicado <= 100");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_excepcion_coherente",
                "(excepcion_evaluador_unico = true AND cantidad_evaluaciones_validas = 1 AND justificacion_evaluador_unico IS NOT NULL AND length(btrim(justificacion_evaluador_unico)) > 0 AND autorizador_excepcion_id IS NOT NULL AND diferencia_entre_evaluadores = 0 AND alerta_discrepancia = false) OR (excepcion_evaluador_unico = false AND cantidad_evaluaciones_validas >= 2 AND justificacion_evaluador_unico IS NULL AND autorizador_excepcion_id IS NULL)");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_alerta_coherente",
                "(excepcion_evaluador_unico = true AND alerta_discrepancia = false) OR (excepcion_evaluador_unico = false AND alerta_discrepancia = (diferencia_entre_evaluadores > umbral_discrepancia_aplicado))");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_revision_coherente",
                "(discrepancia_revisada = false AND comentario_revision_discrepancia IS NULL AND revisado_por_usuario_id IS NULL AND fecha_revision_discrepancia IS NULL) OR (discrepancia_revisada = true AND alerta_discrepancia = true AND comentario_revision_discrepancia IS NOT NULL AND length(btrim(comentario_revision_discrepancia)) > 0 AND revisado_por_usuario_id IS NOT NULL AND fecha_revision_discrepancia IS NOT NULL)");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_clasificacion_valida",
                $"clasificacion IS NULL OR clasificacion IN ({ConfiguracionPersistencia.ValoresSql<ClasificacionRanking>()})");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_ranking_campos_rango",
                "(posicion_ranking IS NULL OR posicion_ranking >= 1) AND (monto_estimado_beca IS NULL OR monto_estimado_beca > 0)");
            tabla.HasCheckConstraint(
                "ck_resultados_consolidados_confirmacion_coherente",
                "(estado_confirmacion_ranking = 'PROVISIONAL' AND posicion_ranking IS NULL AND monto_estimado_beca IS NULL AND clasificacion IS NULL AND fecha_confirmacion_ranking IS NULL AND confirmado_por_usuario_id IS NULL) OR (estado_confirmacion_ranking = 'CONFIRMADO' AND posicion_ranking IS NOT NULL AND monto_estimado_beca IS NOT NULL AND clasificacion IS NOT NULL AND fecha_confirmacion_ranking IS NOT NULL AND confirmado_por_usuario_id IS NOT NULL) OR estado_confirmacion_ranking NOT IN ('PROVISIONAL', 'CONFIRMADO')");
        });

        builder.HasKey(resultado => resultado.Id)
            .HasName("pk_resultados_consolidados_ranking");

        builder.Ignore(resultado => resultado.EventosDominio);
        builder.Ignore(resultado => resultado.EsAdjudicable);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(resultado => resultado.ConvocatoriaId)
            .HasColumnName("convocatoria_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(resultado => resultado.PostulacionId)
            .HasColumnName("postulacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(resultado => resultado.ExcepcionEvaluadorUnico)
            .HasColumnName("excepcion_evaluador_unico")
            .HasColumnType("boolean")
            .IsRequired();

        builder.Property(resultado => resultado.JustificacionEvaluadorUnico)
            .HasColumnName("justificacion_evaluador_unico")
            .HasMaxLength(ResultadoConsolidadoRanking.LongitudMaximaJustificacionExcepcion);

        builder.Property(resultado => resultado.AutorizadorExcepcionId)
            .HasColumnName("autorizador_excepcion_id")
            .HasColumnType("uuid");

        builder.Property(resultado => resultado.CantidadEvaluacionesValidas)
            .HasColumnName("cantidad_evaluaciones_validas")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(resultado => resultado.PuntajeConsolidado)
            .HasColumnName("puntaje_consolidado")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(resultado => resultado.DiferenciaEntreEvaluadores)
            .HasColumnName("diferencia_entre_evaluadores")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(resultado => resultado.UmbralDiscrepanciaAplicado)
            .HasColumnName("umbral_discrepancia_aplicado")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(resultado => resultado.AlertaDiscrepancia)
            .HasColumnName("alerta_discrepancia")
            .HasColumnType("boolean")
            .IsRequired();

        builder.Property(resultado => resultado.DiscrepanciaRevisada)
            .HasColumnName("discrepancia_revisada")
            .HasColumnType("boolean")
            .IsRequired();

        builder.Property(resultado => resultado.ComentarioRevisionDiscrepancia)
            .HasColumnName("comentario_revision_discrepancia")
            .HasMaxLength(ResultadoConsolidadoRanking.LongitudMaximaComentarioRevision);

        builder.Property(resultado => resultado.RevisadoPorUsuarioId)
            .HasColumnName("revisado_por_usuario_id")
            .HasColumnType("uuid");

        builder.Property(resultado => resultado.FechaRevisionDiscrepancia)
            .HasColumnName("fecha_revision_discrepancia")
            .HasColumnType("timestamptz");

        builder.Property(resultado => resultado.EstadoConfirmacionRanking)
            .HasColumnName("estado_confirmacion_ranking")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(resultado => resultado.FechaConsolidacion)
            .HasColumnName("fecha_consolidacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(resultado => resultado.PosicionRanking)
            .HasColumnName("posicion_ranking")
            .HasColumnType("integer");

        builder.Property(resultado => resultado.MontoEstimadoBeca)
            .HasColumnName("monto_estimado_beca")
            .HasPrecision(12, 2);

        builder.Property(resultado => resultado.Clasificacion)
            .HasColumnName("clasificacion")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(20);

        builder.Property(resultado => resultado.FechaConfirmacionRanking)
            .HasColumnName("fecha_confirmacion_ranking")
            .HasColumnType("timestamptz");

        builder.Property(resultado => resultado.ConfirmadoPorUsuarioId)
            .HasColumnName("confirmado_por_usuario_id")
            .HasColumnType("uuid");

        builder.HasIndex(resultado => resultado.PostulacionId)
            .IsUnique()
            .HasDatabaseName("ux_resultados_consolidados_ranking_postulacion");

        builder.HasIndex(resultado => new
        {
            resultado.ConvocatoriaId,
            resultado.PosicionRanking
        })
            .IsUnique()
            .HasFilter("posicion_ranking IS NOT NULL")
            .HasDatabaseName("ux_resultados_consolidados_ranking_convocatoria_posicion");

        builder.HasIndex(resultado => new
        {
            resultado.ConvocatoriaId,
            resultado.PuntajeConsolidado,
            resultado.FechaConsolidacion,
            resultado.Id
        })
            .HasDatabaseName("ix_resultados_consolidados_ranking_convocatoria_puntaje");

        builder.HasIndex(resultado => new
        {
            resultado.ConvocatoriaId,
            resultado.AlertaDiscrepancia,
            resultado.DiscrepanciaRevisada
        })
            .HasDatabaseName("ix_resultados_consolidados_ranking_convocatoria_discrepancia");
    }
}
