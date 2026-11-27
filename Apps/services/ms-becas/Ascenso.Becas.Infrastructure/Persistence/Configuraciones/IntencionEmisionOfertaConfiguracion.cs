using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class IntencionEmisionOfertaConfiguracion
    : IEntityTypeConfiguration<IntencionEmisionOfertaRegistro>
{
    public void Configure(EntityTypeBuilder<IntencionEmisionOfertaRegistro> builder)
    {
        builder.ToTable("intencion_emision_oferta");
        builder.HasKey(valor => valor.OfertaId)
            .HasName("pk_intencion_emision_oferta");
        builder.Property(valor => valor.OfertaId)
            .HasColumnName("oferta_id").HasColumnType("uuid");
        builder.Property(valor => valor.PostulacionId)
            .HasColumnName("postulacion_id").HasColumnType("uuid");
        builder.Property(valor => valor.Payload)
            .HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(valor => valor.CreadoEn)
            .HasColumnName("creado_en").HasColumnType("timestamptz");
        builder.HasIndex(valor => valor.PostulacionId).IsUnique()
            .HasDatabaseName("ux_intencion_emision_oferta_postulacion");
    }
}
