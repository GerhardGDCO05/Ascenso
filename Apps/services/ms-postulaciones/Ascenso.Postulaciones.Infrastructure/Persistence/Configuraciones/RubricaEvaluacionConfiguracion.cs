using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el agregado <see cref="RubricaEvaluacion"/>.
/// </summary>
internal sealed class RubricaEvaluacionConfiguracion : IEntityTypeConfiguration<RubricaEvaluacion>
{
    public void Configure(EntityTypeBuilder<RubricaEvaluacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("rubricas", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_rubricas_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoRubrica>()})");
        });

        builder.HasKey(rubrica => rubrica.Id)
            .HasName("pk_rubricas");

        builder.Ignore(rubrica => rubrica.EventosDominio);
        builder.Ignore(rubrica => rubrica.SumaPesos);
        builder.Ignore(rubrica => rubrica.DiferenciaPara100);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(rubrica => rubrica.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(rubrica => rubrica.ConvocatoriaId)
            .HasColumnName("convocatoria_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(rubrica => rubrica.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(RubricaEvaluacion.LongitudMaximaNombre)
            .IsRequired();

        builder.Property(rubrica => rubrica.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(rubrica => rubrica.FechaPublicacion)
            .HasColumnName("fecha_publicacion")
            .HasColumnType("timestamptz");

        builder.HasMany(rubrica => rubrica.Criterios)
            .WithOne()
            .HasForeignKey(criterio => criterio.RubricaEvaluacionId)
            .HasConstraintName("fk_criterios_evaluacion_rubricas")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(rubrica => rubrica.Criterios)
            .HasField("_criterios")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(rubrica => new { rubrica.ProgramaBecaId, rubrica.ConvocatoriaId })
            .IsUnique()
            .HasDatabaseName("ux_rubricas_programa_convocatoria");
    }
}
