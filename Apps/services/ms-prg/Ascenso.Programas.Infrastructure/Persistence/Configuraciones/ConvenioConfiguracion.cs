using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Universidades.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class ConvenioConfiguracion : IEntityTypeConfiguration<Convenio>
{
    public void Configure(EntityTypeBuilder<Convenio> builder)
    {
        builder.ToTable("convenios_universidad", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_convenio_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoConvenio>()})");
            tabla.HasCheckConstraint(
                "ck_convenio_fechas_validas",
                "fecha_fin > fecha_inicio");
        });

        builder.HasKey(convenio => convenio.Id).HasName("pk_convenios_universidad");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(convenio => convenio.UniversidadAliadaId)
            .HasColumnName("universidad_aliada_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(convenio => convenio.NumeroConvenio)
            .HasColumnName("numero_convenio")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(convenio => convenio.TerminosResumen)
            .HasColumnName("terminos_resumen")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(convenio => convenio.FechaInicio)
            .HasColumnName("fecha_inicio")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(convenio => convenio.FechaFin)
            .HasColumnName("fecha_fin")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(convenio => convenio.UrlDocumentoPdfFirebase)
            .HasColumnName("url_documento_pdf_firebase")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(convenio => convenio.PermiteIntercambioAutomaticoDatos)
            .HasColumnName("permite_intercambio_automatico_datos")
            .IsRequired();

        builder.Property(convenio => convenio.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(convenio => new { convenio.UniversidadAliadaId, convenio.NumeroConvenio })
            .HasDatabaseName("ix_convenio_universidad_numero");
    }
}
