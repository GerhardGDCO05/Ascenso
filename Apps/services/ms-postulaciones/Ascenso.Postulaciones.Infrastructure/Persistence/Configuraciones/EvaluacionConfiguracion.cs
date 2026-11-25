using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el agregado <see cref="Evaluacion"/>.
/// </summary>
internal sealed class EvaluacionConfiguracion : IEntityTypeConfiguration<Evaluacion>
{
    public const int LongitudMaximaMotivoInvalidacion = 1000;

    public void Configure(EntityTypeBuilder<Evaluacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("evaluaciones", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_evaluaciones_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoEvaluacion>()})");
            tabla.HasCheckConstraint(
                "ck_evaluaciones_puntaje_total_rango",
                "puntaje_total_ponderado IS NULL OR (puntaje_total_ponderado >= 0 AND puntaje_total_ponderado <= 100)");
            tabla.HasCheckConstraint(
                "ck_evaluaciones_envio_coherente",
                "(estado = 'BORRADOR' AND fecha_envio IS NULL AND puntaje_total_ponderado IS NULL AND motivo_invalidacion IS NULL) OR (estado = 'ENVIADA_DEFINITIVA' AND fecha_envio IS NOT NULL AND puntaje_total_ponderado IS NOT NULL AND motivo_invalidacion IS NULL) OR (estado = 'INVALIDADA_POR_CONFLICTO' AND motivo_invalidacion IS NOT NULL AND length(btrim(motivo_invalidacion)) > 0)");
            tabla.HasCheckConstraint(
                "ck_evaluaciones_comentarios_generales_no_vacios",
                "comentarios_generales IS NULL OR length(btrim(comentarios_generales)) > 0");
        });

        builder.HasKey(evaluacion => evaluacion.Id)
            .HasName("pk_evaluaciones");

        builder.Ignore(evaluacion => evaluacion.EventosDominio);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(evaluacion => evaluacion.AsignacionEvaluacionId)
            .HasColumnName("asignacion_evaluacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(evaluacion => evaluacion.PostulacionId)
            .HasColumnName("postulacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(evaluacion => evaluacion.ConvocatoriaId)
            .HasColumnName("convocatoria_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(evaluacion => evaluacion.RubricaEvaluacionId)
            .HasColumnName("rubrica_evaluacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(evaluacion => evaluacion.EvaluadorId)
            .HasColumnName("evaluador_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(evaluacion => evaluacion.FechaInicio)
            .HasColumnName("fecha_inicio")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(evaluacion => evaluacion.FechaEnvio)
            .HasColumnName("fecha_envio")
            .HasColumnType("timestamptz");

        builder.Property(evaluacion => evaluacion.PuntajeTotalPonderado)
            .HasColumnName("puntaje_total_ponderado")
            .HasPrecision(5, 2);

        builder.Property(evaluacion => evaluacion.ComentariosGenerales)
            .HasColumnName("comentarios_generales")
            .HasMaxLength(Evaluacion.LongitudMaximaComentariosGenerales);

        builder.Property(evaluacion => evaluacion.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(evaluacion => evaluacion.MotivoInvalidacion)
            .HasColumnName("motivo_invalidacion")
            .HasMaxLength(LongitudMaximaMotivoInvalidacion);

        builder.HasOne<AsignacionEvaluacion>()
            .WithMany()
            .HasForeignKey(evaluacion => evaluacion.AsignacionEvaluacionId)
            .HasConstraintName("fk_evaluaciones_asignaciones_evaluacion")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RubricaEvaluacion>()
            .WithMany()
            .HasForeignKey(evaluacion => evaluacion.RubricaEvaluacionId)
            .HasConstraintName("fk_evaluaciones_rubricas")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(evaluacion => evaluacion.Calificaciones)
            .WithOne()
            .HasForeignKey(calificacion => calificacion.EvaluacionId)
            .HasConstraintName("fk_calificaciones_criterio_evaluaciones")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(evaluacion => evaluacion.Calificaciones)
            .HasField("_calificaciones")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(evaluacion => evaluacion.AsignacionEvaluacionId)
            .IsUnique()
            .HasDatabaseName("ux_evaluaciones_asignacion_evaluacion");

        builder.HasIndex(evaluacion => new
        {
            evaluacion.EvaluadorId,
            evaluacion.Estado,
            evaluacion.FechaUltimaModificacion
        })
            .HasDatabaseName("ix_evaluaciones_evaluador_estado");

        builder.HasIndex(evaluacion => new { evaluacion.PostulacionId, evaluacion.Estado })
            .HasDatabaseName("ix_evaluaciones_postulacion_estado");
    }
}
