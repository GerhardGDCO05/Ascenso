using System.Text.Json;
using Ascenso.Programas.Domain.Convocatorias.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class DocumentoRequeridoConfiguracion : IEntityTypeConfiguration<DocumentoRequerido>
{
    public void Configure(EntityTypeBuilder<DocumentoRequerido> builder)
    {
        builder.ToTable("documentos_requeridos", tabla =>
        {
            tabla.HasCheckConstraint("ck_documento_requerido_tamanio_positivo", "tamanio_maximo_mb > 0");
        });

        builder.HasKey(documento => documento.Id).HasName("pk_documentos_requeridos");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(documento => documento.ConvocatoriaId)
            .HasColumnName("convocatoria_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(documento => documento.TipoDocumento)
            .HasColumnName("tipo_documento")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(documento => documento.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(documento => documento.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(1000);

        builder.Property(documento => documento.Obligatorio)
            .HasColumnName("obligatorio")
            .IsRequired();

        builder.Property(documento => documento.TamanioMaximoMb)
            .HasColumnName("tamanio_maximo_mb")
            .IsRequired();

        var mimeListConverter = new ValueConverter<IReadOnlyList<string>, string>(
            lista => JsonSerializer.Serialize(lista, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<string[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string>());

        var mimeListComparer = new ValueComparer<IReadOnlyList<string>>(
            (izquierda, derecha) => ReferenceEquals(izquierda, derecha)
                || (izquierda != null && derecha != null && izquierda.SequenceEqual(derecha)),
            lista => lista.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            lista => lista.ToArray());

        var mimesProp = builder.Property(documento => documento.TiposMimePermitidos)
            .HasColumnName("tipos_mime_permitidos")
            .HasColumnType("jsonb")
            .HasConversion(mimeListConverter)
            .IsRequired();
        mimesProp.Metadata.SetValueComparer(mimeListComparer);

        builder.HasIndex(documento => documento.ConvocatoriaId)
            .HasDatabaseName("ix_documento_requerido_convocatoria");
    }
}
