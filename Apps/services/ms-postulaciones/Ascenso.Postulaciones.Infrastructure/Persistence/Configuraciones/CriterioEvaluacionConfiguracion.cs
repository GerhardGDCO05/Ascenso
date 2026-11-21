using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para la entidad <see cref="CriterioEvaluacion"/>.
/// </summary>
internal sealed class CriterioEvaluacionConfiguracion : IEntityTypeConfiguration<CriterioEvaluacion>
{
    public void Configure(EntityTypeBuilder<CriterioEvaluacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("criterios_evaluacion", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_criterios_evaluacion_peso_rango",
                "peso_porcentual > 0 AND peso_porcentual <= 100");
            tabla.HasCheckConstraint(
                "ck_criterios_evaluacion_puntaje_rango",
                "puntaje_minimo >= 0 AND puntaje_maximo <= 1000 AND puntaje_minimo < puntaje_maximo");
            tabla.HasCheckConstraint(
                "ck_criterios_evaluacion_orden_positivo",
                "orden > 0");
        });

        builder.HasKey(criterio => criterio.Id)
            .HasName("pk_criterios_evaluacion");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(criterio => criterio.RubricaEvaluacionId)
            .HasColumnName("rubrica_evaluacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(criterio => criterio.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(CriterioEvaluacion.LongitudMaximaNombre)
            .IsRequired();

        builder.Property(criterio => criterio.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(CriterioEvaluacion.LongitudMaximaDescripcion)
            .IsRequired();

        builder.Property(criterio => criterio.PesoPorcentual)
            .HasColumnName("peso_porcentual")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(criterio => criterio.PuntajeMinimo)
            .HasColumnName("puntaje_minimo")
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(criterio => criterio.PuntajeMaximo)
            .HasColumnName("puntaje_maximo")
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(criterio => criterio.Orden)
            .HasColumnName("orden")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasIndex(criterio => new { criterio.RubricaEvaluacionId, criterio.Orden })
            .IsUnique()
            .HasDatabaseName("ux_criterios_evaluacion_rubrica_orden");
    }
}
