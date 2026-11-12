using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class CompromisoBecarioConfiguracion : IEntityTypeConfiguration<CompromisoBecario>
{
    public void Configure(EntityTypeBuilder<CompromisoBecario> builder)
    {
        builder.ToTable("compromiso_becario", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_compromiso_becario_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoCompromiso>()})");
            tabla.HasCheckConstraint(
                "ck_compromiso_becario_intento_valido",
                "numero_intento_carga >= 0");
        });

        builder.HasKey(compromiso => compromiso.Id).HasName("pk_compromiso_becario");

        ConfiguracionPersistencia.ConfigurarAuditoria(builder);

        builder.Property(compromiso => compromiso.BecaId)
            .HasColumnName("beca_id")
            .HasColumnType("uuid");

        builder.Property(compromiso => compromiso.UrlPlantillaGeneradaFirebase)
            .HasColumnName("url_plantilla_generada_firebase")
            .HasMaxLength(2048);

        builder.Property(compromiso => compromiso.UrlPdfFirmadoFirebase)
            .HasColumnName("url_pdf_firmado_firebase")
            .HasMaxLength(2048);

        builder.Property(compromiso => compromiso.HashDocumentoSha256)
            .HasColumnName("hash_documento_sha256")
            .HasMaxLength(64)
            .IsFixedLength();

        builder.Property(compromiso => compromiso.NumeroIntentoCarga)
            .HasColumnName("numero_intento_carga");

        builder.Property(compromiso => compromiso.IpCargaFirma)
            .HasColumnName("ip_carga_firma")
            .HasMaxLength(45);

        builder.Property(compromiso => compromiso.FechaCargaFirma)
            .HasColumnName("fecha_carga_firma")
            .HasColumnType("timestamptz");

        builder.Property(compromiso => compromiso.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(30);

        builder.Property(compromiso => compromiso.ObservacionesRevision)
            .HasColumnName("observaciones_revision")
            .HasMaxLength(2000);

        builder.Property(compromiso => compromiso.RevisadoPorCoordinadorId)
            .HasColumnName("revisado_por_coordinador_id")
            .HasColumnType("uuid");

        builder.Property(compromiso => compromiso.FechaRevision)
            .HasColumnName("fecha_revision")
            .HasColumnType("timestamptz");

        builder.HasIndex(compromiso => compromiso.BecaId)
            .IsUnique()
            .HasDatabaseName("ux_compromiso_becario_beca");
    }
}
