using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class AsignacionFondoProgramaConfiguracion : IEntityTypeConfiguration<AsignacionFondoPrograma>
{
    public void Configure(EntityTypeBuilder<AsignacionFondoPrograma> builder)
    {
        builder.ToTable("asignaciones_fondo_programa", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_asignacion_fondo_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoAsignacionFondo>()})");
            tabla.HasCheckConstraint("ck_asignacion_fondo_monto_no_negativo", "monto_asignado >= 0");
        });

        builder.HasKey(asignacion => asignacion.Id).HasName("pk_asignaciones_fondo_programa");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(asignacion => asignacion.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(asignacion => asignacion.DonanteId)
            .HasColumnName("donante_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(asignacion => asignacion.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(asignacion => asignacion.MontoAsignado)
            .ComoDinero("monto_asignado")
            .IsRequired();

        builder.Property(asignacion => asignacion.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(asignacion => asignacion.FechaAsignacion)
            .HasColumnName("fecha_asignacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(asignacion => asignacion.AutorizadoPorUsuarioId)
            .HasColumnName("autorizado_por_usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(asignacion => new
        {
            asignacion.ProgramaBecaId,
            asignacion.DonanteId,
            asignacion.PeriodoAcademicoId
        }).HasDatabaseName("ix_asignacion_fondo_programa_donante_periodo");
    }
}
