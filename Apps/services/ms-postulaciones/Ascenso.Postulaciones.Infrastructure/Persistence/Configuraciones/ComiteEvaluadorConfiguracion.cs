using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el agregado <see cref="ComiteEvaluador"/>.
/// </summary>
internal sealed class ComiteEvaluadorConfiguracion : IEntityTypeConfiguration<ComiteEvaluador>
{
    public void Configure(EntityTypeBuilder<ComiteEvaluador> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("comites_evaluadores");

        builder.HasKey(comite => comite.Id)
            .HasName("pk_comites_evaluadores");

        builder.Ignore(comite => comite.EventosDominio);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(comite => comite.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(comite => comite.ConvocatoriaId)
            .HasColumnName("convocatoria_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(comite => comite.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(ComiteEvaluador.LongitudMaximaNombre)
            .IsRequired();

        builder.Property(comite => comite.JefaturaUsuarioId)
            .HasColumnName("jefatura_usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(comite => comite.FechaConformacion)
            .HasColumnName("fecha_conformacion")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(comite => comite.ConformadoPorUsuarioId)
            .HasColumnName("conformado_por_usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(comite => comite.Activo)
            .HasColumnName("activo")
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasMany(comite => comite.Miembros)
            .WithOne()
            .HasForeignKey(miembro => miembro.ComiteEvaluadorId)
            .HasConstraintName("fk_miembros_comite_comites")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(comite => comite.Miembros)
            .HasField("_miembros")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(comite => new { comite.ProgramaBecaId, comite.ConvocatoriaId })
            .IsUnique()
            .HasDatabaseName("ux_comites_programa_convocatoria");
    }
}
