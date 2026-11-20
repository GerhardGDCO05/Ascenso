using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Postulantes.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

internal sealed class MiembroFamiliarConfiguracion : IEntityTypeConfiguration<MiembroFamiliar>
{
    public void Configure(EntityTypeBuilder<MiembroFamiliar> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("miembros_familiares", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_miembro_familiar_parentesco_valido",
                $"parentesco IN ({ConfiguracionPersistencia.ValoresSql<Parentesco>()})");
            tabla.HasCheckConstraint(
                "ck_miembro_familiar_edad_rango",
                "edad >= 0 AND edad <= 120");
            tabla.HasCheckConstraint(
                "ck_miembro_familiar_ocupacion_valida",
                $"ocupacion IN ({ConfiguracionPersistencia.ValoresSql<Ocupacion>()})");
            tabla.HasCheckConstraint(
                "ck_miembro_familiar_ingreso_no_negativo",
                "ingreso_mensual >= 0");
        });

        builder.HasKey(miembro => miembro.Id)
            .HasName("pk_miembros_familiares");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(miembro => miembro.PostulanteId)
            .HasColumnName("postulante_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(miembro => miembro.Parentesco)
            .HasColumnName("parentesco")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(miembro => miembro.Edad)
            .HasColumnName("edad")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(miembro => miembro.Ocupacion)
            .HasColumnName("ocupacion")
            .ComoCodigoEnum()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(miembro => miembro.IngresoMensual)
            .HasColumnName("ingreso_mensual")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(miembro => miembro.PostulanteId)
            .HasDatabaseName("ix_miembros_familiares_postulante_id");
    }
}
