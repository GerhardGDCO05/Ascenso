using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class ConvenioRefConfiguracion : IEntityTypeConfiguration<ConvenioRef>
{
    public void Configure(EntityTypeBuilder<ConvenioRef> builder)
    {
        builder.ToTable("convenio_ref");

        builder.HasKey(convenio => convenio.UniversidadAliadaId).HasName("pk_convenio_ref");

        builder.Property(convenio => convenio.UniversidadAliadaId)
            .HasColumnName("universidad_aliada_id")
            .HasColumnType("uuid");

        builder.Property(convenio => convenio.ConvenioId)
            .HasColumnName("convenio_id")
            .HasColumnType("uuid");

        builder.Property(convenio => convenio.Estado)
            .HasColumnName("estado")
            .HasMaxLength(30);

        builder.Property(convenio => convenio.FechaFin)
            .HasColumnName("fecha_fin")
            .HasColumnType("date");

        builder.Property(convenio => convenio.UltimoEventoId)
            .HasColumnName("ultimo_evento_id")
            .HasColumnType("uuid");

        builder.Property(convenio => convenio.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz");

        builder.Property(convenio => convenio.FechaUltimaActualizacion)
            .HasColumnName("fecha_ultima_actualizacion")
            .HasColumnType("timestamptz");
    }
}
