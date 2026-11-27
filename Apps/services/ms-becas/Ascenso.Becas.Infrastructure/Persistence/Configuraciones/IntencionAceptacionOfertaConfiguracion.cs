using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class IntencionAceptacionOfertaConfiguracion
    : IEntityTypeConfiguration<IntencionAceptacionOfertaRegistro>
{
    public void Configure(EntityTypeBuilder<IntencionAceptacionOfertaRegistro> builder)
    {
        builder.ToTable("intencion_aceptacion_oferta");
        builder.HasKey(valor => valor.OfertaId)
            .HasName("pk_intencion_aceptacion_oferta");
        builder.Property(valor => valor.OfertaId)
            .HasColumnName("oferta_id").HasColumnType("uuid");
        builder.Property(valor => valor.BecaId)
            .HasColumnName("beca_id").HasColumnType("uuid");
        builder.Property(valor => valor.Payload)
            .HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(valor => valor.CreadoEn)
            .HasColumnName("creado_en").HasColumnType("timestamptz");
        builder.HasIndex(valor => valor.BecaId).IsUnique()
            .HasDatabaseName("ux_intencion_aceptacion_oferta_beca");
    }
}
