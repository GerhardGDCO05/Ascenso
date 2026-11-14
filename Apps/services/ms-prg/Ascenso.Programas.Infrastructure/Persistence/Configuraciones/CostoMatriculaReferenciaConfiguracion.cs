using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Universidades.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class CostoMatriculaReferenciaConfiguracion : IEntityTypeConfiguration<CostoMatriculaReferencia>
{
    public void Configure(EntityTypeBuilder<CostoMatriculaReferencia> builder)
    {
        builder.ToTable("costos_matricula_referencia", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_costo_matricula_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoVersionCosto>()})");
            tabla.HasCheckConstraint(
                "ck_costo_matricula_monto_positivo",
                "monto_referencia_matricula > 0");
            tabla.HasCheckConstraint(
                "ck_costo_matricula_version_positiva",
                "numero_version > 0");
        });

        builder.HasKey(costo => costo.Id).HasName("pk_costos_matricula_referencia");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(costo => costo.CarreraElegibleId)
            .HasColumnName("carrera_elegible_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(costo => costo.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(costo => costo.NumeroVersion)
            .HasColumnName("numero_version")
            .IsRequired();

        builder.Property(costo => costo.MontoReferenciaMatricula)
            .ComoDinero("monto_referencia_matricula");

        builder.Property(costo => costo.Moneda)
            .HasColumnName("moneda")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(costo => costo.FechaVigenciaDesde)
            .HasColumnName("fecha_vigencia_desde")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(costo => costo.FechaVigenciaHasta)
            .HasColumnName("fecha_vigencia_hasta")
            .HasColumnType("timestamptz");

        builder.Property(costo => costo.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(costo => new { costo.CarreraElegibleId, costo.PeriodoAcademicoId, costo.NumeroVersion })
            .IsUnique()
            .HasDatabaseName("ux_costo_matricula_carrera_periodo_version");
    }
}
