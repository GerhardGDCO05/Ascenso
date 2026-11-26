using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class ReporteAcademicoConfiguracion : IEntityTypeConfiguration<ReporteAcademico>
{
    public void Configure(EntityTypeBuilder<ReporteAcademico> builder)
    {
        builder.ToTable("reporte_academico", tabla =>
        {
            tabla.HasCheckConstraint("ck_reporte_academico_promedio",
                "promedio_periodo >= 0 AND promedio_periodo <= 20");
            tabla.HasCheckConstraint("ck_reporte_academico_materias",
                "materias_aprobadas >= 0 AND materias_reprobadas >= 0 AND materias_cursando >= 0 AND creditos_aprobados >= 0");
            tabla.HasCheckConstraint("ck_reporte_academico_horas",
                "horas_servicio_cumplidas IS NULL OR horas_servicio_cumplidas >= 0");
            tabla.HasCheckConstraint("ck_reporte_academico_estado_matricula",
                $"estado_matricula IN ({ConfiguracionPersistencia.ValoresSql<EstadoMatricula>()})");
            tabla.HasCheckConstraint("ck_reporte_academico_fuente",
                $"fuente_entrega IN ({ConfiguracionPersistencia.ValoresSql<FuenteReporte>()})");
            tabla.HasCheckConstraint("ck_reporte_academico_estado",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoReporteAcademico>()})");
        });

        builder.HasKey(reporte => reporte.Id).HasName("pk_reporte_academico");
        builder.Ignore(reporte => reporte.EventosDominio);
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);

        builder.Property(reporte => reporte.BecaId).HasColumnName("beca_id").HasColumnType("uuid");
        builder.Property(reporte => reporte.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id").HasColumnType("uuid");
        builder.Property(reporte => reporte.PromedioPeriodo)
            .HasColumnName("promedio_periodo").HasPrecision(4, 2);
        builder.Property(reporte => reporte.MateriasAprobadas).HasColumnName("materias_aprobadas");
        builder.Property(reporte => reporte.MateriasReprobadas).HasColumnName("materias_reprobadas");
        builder.Property(reporte => reporte.MateriasCursando).HasColumnName("materias_cursando");
        builder.Property(reporte => reporte.CreditosAprobados).HasColumnName("creditos_aprobados");
        builder.Property(reporte => reporte.HorasServicioCumplidas)
            .HasColumnName("horas_servicio_cumplidas");
        builder.Property(reporte => reporte.UrlComprobanteServicio)
            .HasColumnName("url_comprobante_servicio").HasMaxLength(2048);
        builder.Property(reporte => reporte.HashComprobanteServicioSha256)
            .HasColumnName("hash_comprobante_servicio_sha256").HasMaxLength(64);
        builder.Property(reporte => reporte.EstadoMatricula)
            .HasColumnName("estado_matricula").ComoCodigoEnum().HasMaxLength(30);
        builder.Property(reporte => reporte.UrlComprobanteNotasFirebase)
            .HasColumnName("url_comprobante_notas_firebase").HasMaxLength(2048);
        builder.Property(reporte => reporte.HashComprobanteSha256)
            .HasColumnName("hash_comprobante_sha256").HasMaxLength(64);
        builder.Property(reporte => reporte.TamanioComprobanteBytes)
            .HasColumnName("tamanio_comprobante_bytes");
        builder.Property(reporte => reporte.TipoMimeComprobante)
            .HasColumnName("tipo_mime_comprobante").HasMaxLength(100);
        builder.Property(reporte => reporte.FuenteEntrega)
            .HasColumnName("fuente_entrega").ComoCodigoEnum().HasMaxLength(20);
        builder.Property(reporte => reporte.UniversidadAliadaId)
            .HasColumnName("universidad_aliada_id").HasColumnType("uuid");
        builder.Property(reporte => reporte.FechaEntrega)
            .HasColumnName("fecha_entrega").HasColumnType("timestamptz");
        builder.Property(reporte => reporte.FechaLimitePeriodo)
            .HasColumnName("fecha_limite_periodo").HasColumnType("date");
        builder.Property(reporte => reporte.EntregaTardia)
            .HasColumnName("entrega_tardia");
        builder.Property(reporte => reporte.Estado)
            .HasColumnName("estado").ComoCodigoEnum().HasMaxLength(20);
        builder.Property(reporte => reporte.RevisadoPorUsuarioId)
            .HasColumnName("revisado_por_usuario_id").HasColumnType("uuid");
        builder.Property(reporte => reporte.FechaRevision)
            .HasColumnName("fecha_revision").HasColumnType("timestamptz");

        builder.HasOne<Beca>().WithMany().HasForeignKey(reporte => reporte.BecaId)
            .HasConstraintName("fk_reporte_academico_beca").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(reporte => reporte.Evaluacion).WithOne()
            .HasForeignKey<EvaluacionPermanencia>(evaluacion => evaluacion.ReporteAcademicoId)
            .HasConstraintName("fk_evaluacion_permanencia_reporte")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(reporte => new { reporte.BecaId, reporte.PeriodoAcademicoId })
            .HasDatabaseName("ix_reporte_academico_beca_periodo");
    }
}
