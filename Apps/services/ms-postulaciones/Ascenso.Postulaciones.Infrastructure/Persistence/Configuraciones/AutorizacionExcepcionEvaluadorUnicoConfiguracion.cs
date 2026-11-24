using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el registro inmutable <see cref="AutorizacionExcepcionEvaluadorUnico"/>.
/// </summary>
internal sealed class AutorizacionExcepcionEvaluadorUnicoConfiguracion : IEntityTypeConfiguration<AutorizacionExcepcionEvaluadorUnico>
{
    public void Configure(EntityTypeBuilder<AutorizacionExcepcionEvaluadorUnico> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("excepciones_evaluador_unico", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_excepciones_justificacion_no_vacia",
                "char_length(btrim(justificacion)) >= 10");
        });

        builder.HasKey(autorizacion => autorizacion.Id)
            .HasName("pk_excepciones_evaluador_unico");

        builder.Property(autorizacion => autorizacion.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(autorizacion => autorizacion.PostulacionId)
            .HasColumnName("postulacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(autorizacion => autorizacion.Justificacion)
            .HasColumnName("justificacion")
            .HasColumnType("varchar(1000)")
            .HasMaxLength(AutorizacionExcepcionEvaluadorUnico.LongitudMaximaJustificacion)
            .IsRequired();

        builder.Property(autorizacion => autorizacion.UsuarioAutorId)
            .HasColumnName("usuario_autorizador_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(autorizacion => autorizacion.FechaCreacion)
            .HasColumnName("fecha_autorizacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(autorizacion => autorizacion.CorrelacionId)
            .HasColumnName("correlacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(autorizacion => autorizacion.PostulacionId)
            .IsUnique()
            .HasDatabaseName("ux_excepciones_evaluador_unico_postulacion");
    }
}
