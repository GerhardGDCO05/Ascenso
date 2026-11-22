using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para la entidad <see cref="MiembroComite"/>.
/// </summary>
internal sealed class MiembroComiteConfiguracion : IEntityTypeConfiguration<MiembroComite>
{
    public void Configure(EntityTypeBuilder<MiembroComite> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("miembros_comite", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_miembros_comite_tipo_valido",
                $"tipo IN ({ConfiguracionPersistencia.ValoresSql<TipoEvaluador>()})");
            tabla.HasCheckConstraint(
                "ck_miembros_comite_rol_valido",
                $"rol IN ({ConfiguracionPersistencia.ValoresSql<RolMiembroComite>()})");
            tabla.HasCheckConstraint(
                "ck_miembros_comite_desvinculacion_coherente",
                "(activo AND fecha_desvinculacion IS NULL) OR (NOT activo AND fecha_desvinculacion IS NOT NULL AND fecha_desvinculacion >= fecha_asignacion)");
            tabla.HasCheckConstraint(
                "ck_miembros_comite_jefatura_interna",
                "rol <> 'JEFATURA' OR tipo = 'INTERNO'");
        });

        builder.HasKey(miembro => miembro.Id)
            .HasName("pk_miembros_comite");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(miembro => miembro.ComiteEvaluadorId)
            .HasColumnName("comite_evaluador_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(miembro => miembro.UsuarioEvaluadorId)
            .HasColumnName("usuario_evaluador_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(miembro => miembro.Tipo)
            .HasColumnName("tipo")
            .ComoCodigoEnum()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(miembro => miembro.Rol)
            .HasColumnName("rol")
            .ComoCodigoEnum()
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(miembro => miembro.FechaAsignacion)
            .HasColumnName("fecha_asignacion")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(miembro => miembro.FechaDesvinculacion)
            .HasColumnName("fecha_desvinculacion")
            .HasColumnType("date");

        builder.Property(miembro => miembro.Activo)
            .HasColumnName("activo")
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasIndex(miembro => new { miembro.ComiteEvaluadorId, miembro.UsuarioEvaluadorId })
            .IsUnique()
            .HasFilter("activo")
            .HasDatabaseName("ux_miembros_comite_usuario_activo");

        builder.HasIndex(miembro => miembro.ComiteEvaluadorId)
            .IsUnique()
            .HasFilter("activo AND rol = 'JEFATURA'")
            .HasDatabaseName("ux_miembros_comite_jefatura_activa");
    }
}
