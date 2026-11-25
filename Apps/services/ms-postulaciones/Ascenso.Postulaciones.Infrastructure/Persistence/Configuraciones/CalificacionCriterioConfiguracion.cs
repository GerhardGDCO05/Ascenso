using Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para la entidad <see cref="CalificacionCriterio"/>.
/// </summary>
internal sealed class CalificacionCriterioConfiguracion : IEntityTypeConfiguration<CalificacionCriterio>
{
    public void Configure(EntityTypeBuilder<CalificacionCriterio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("calificaciones_criterio", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_calificaciones_criterio_puntaje_rango",
                "puntaje_asignado >= 0 AND puntaje_asignado <= 1000");
            tabla.HasCheckConstraint(
                "ck_calificaciones_criterio_comentario_no_vacio",
                "comentario IS NULL OR length(btrim(comentario)) > 0");
        });

        builder.HasKey(calificacion => calificacion.Id)
            .HasName("pk_calificaciones_criterio");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(calificacion => calificacion.EvaluacionId)
            .HasColumnName("evaluacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(calificacion => calificacion.CriterioEvaluacionId)
            .HasColumnName("criterio_evaluacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(calificacion => calificacion.PuntajeAsignado)
            .HasColumnName("puntaje_asignado")
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(calificacion => calificacion.Comentario)
            .HasColumnName("comentario")
            .HasMaxLength(DatosCalificacionCriterio.LongitudMaximaComentario);

        builder.HasOne<CriterioEvaluacion>()
            .WithMany()
            .HasForeignKey(calificacion => calificacion.CriterioEvaluacionId)
            .HasConstraintName("fk_calificaciones_criterio_criterios_evaluacion")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(calificacion => new { calificacion.EvaluacionId, calificacion.CriterioEvaluacionId })
            .IsUnique()
            .HasDatabaseName("ux_calificaciones_criterio_evaluacion_criterio");
    }
}
