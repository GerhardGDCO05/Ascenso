using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class CostoMatriculaRefConfiguracion : IEntityTypeConfiguration<CostoMatriculaRef>
{
    public void Configure(EntityTypeBuilder<CostoMatriculaRef> builder)
    {
        builder.ToTable("costo_matricula_ref", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_costo_matricula_ref_monto_no_negativo",
                "monto_referencia_matricula >= 0");
            tabla.HasCheckConstraint(
                "ck_costo_matricula_ref_version_valida",
                "numero_version > 0");
            tabla.HasCheckConstraint(
                "ck_costo_matricula_ref_fechas_coherentes",
                "fecha_vigencia_hasta IS NULL OR fecha_vigencia_hasta >= fecha_vigencia_desde");
        });

        builder.HasKey(costo => costo.CostoMatriculaReferenciaId).HasName("pk_costo_matricula_ref");

        builder.Property(costo => costo.CostoMatriculaReferenciaId)
            .HasColumnName("costo_matricula_referencia_id")
            .HasColumnType("uuid");

        builder.Property(costo => costo.CarreraId)
            .HasColumnName("carrera_id")
            .HasColumnType("uuid");

        builder.Property(costo => costo.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .HasColumnType("uuid");

        builder.Property(costo => costo.MontoReferenciaMatricula)
            .ConfigurarMonto("monto_referencia_matricula");

        builder.Property(costo => costo.Moneda)
            .HasColumnName("moneda")
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(costo => costo.NumeroVersion)
            .HasColumnName("numero_version");

        builder.Property(costo => costo.FechaVigenciaDesde)
            .HasColumnName("fecha_vigencia_desde")
            .HasColumnType("date");

        builder.Property(costo => costo.FechaVigenciaHasta)
            .HasColumnName("fecha_vigencia_hasta")
            .HasColumnType("date");

        builder.Property(costo => costo.Estado)
            .HasColumnName("estado")
            .HasMaxLength(30);

        builder.Property(costo => costo.UltimoEventoId)
            .HasColumnName("ultimo_evento_id")
            .HasColumnType("uuid");

        builder.Property(costo => costo.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz");

        builder.Property(costo => costo.FechaUltimaActualizacion)
            .HasColumnName("fecha_ultima_actualizacion")
            .HasColumnType("timestamptz");
    }
}
