using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el agregado <see cref="AsignacionEvaluacion"/>.
/// </summary>
internal sealed class AsignacionEvaluacionConfiguracion : IEntityTypeConfiguration<AsignacionEvaluacion>
{
    public void Configure(EntityTypeBuilder<AsignacionEvaluacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("asignaciones_evaluacion", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_asignaciones_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoAsignacionEvaluacion>()})");
            tabla.HasCheckConstraint(
                "ck_asignaciones_fecha_limite_posterior",
                "fecha_limite_evaluacion > fecha_asignacion");
        });

        builder.HasKey(asignacion => asignacion.Id)
            .HasName("pk_asignaciones_evaluacion");

        builder.Ignore(asignacion => asignacion.EventosDominio);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(asignacion => asignacion.ComiteEvaluadorId)
            .HasColumnName("comite_evaluador_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(asignacion => asignacion.ConvocatoriaId)
            .HasColumnName("convocatoria_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(asignacion => asignacion.PostulacionId)
            .HasColumnName("postulacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(asignacion => asignacion.MiembroComiteId)
            .HasColumnName("miembro_comite_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(asignacion => asignacion.UsuarioEvaluadorId)
            .HasColumnName("usuario_evaluador_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(asignacion => asignacion.FechaAsignacion)
            .HasColumnName("fecha_asignacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(asignacion => asignacion.FechaLimiteEvaluacion)
            .HasColumnName("fecha_limite_evaluacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(asignacion => asignacion.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne<ComiteEvaluador>()
            .WithMany()
            .HasForeignKey(asignacion => asignacion.ComiteEvaluadorId)
            .HasConstraintName("fk_asignaciones_comites")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MiembroComite>()
            .WithMany()
            .HasForeignKey(asignacion => asignacion.MiembroComiteId)
            .HasConstraintName("fk_asignaciones_miembros_comite")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(asignacion => new { asignacion.PostulacionId, asignacion.UsuarioEvaluadorId })
            .IsUnique()
            .HasFilter("estado <> 'REASIGNADA_POR_CONFLICTO'")
            .HasDatabaseName("ux_asignaciones_postulacion_evaluador_vigente");

        builder.HasIndex(asignacion => new
        {
            asignacion.UsuarioEvaluadorId,
            asignacion.Estado,
            asignacion.FechaLimiteEvaluacion
        })
            .HasDatabaseName("ix_asignaciones_evaluador_estado");

        builder.HasIndex(asignacion => new { asignacion.ComiteEvaluadorId, asignacion.Estado })
            .HasDatabaseName("ix_asignaciones_comite_estado");

        builder.HasIndex(asignacion => asignacion.PostulacionId)
            .HasDatabaseName("ix_asignaciones_postulacion");
    }
}
