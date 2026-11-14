using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class ReservaPresupuestariaConfiguracion : IEntityTypeConfiguration<ReservaPresupuestaria>
{
    public void Configure(EntityTypeBuilder<ReservaPresupuestaria> builder)
    {
        builder.ToTable("reservas_presupuestarias", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_reserva_presupuestaria_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoReservaPresupuestaria>()})");
            tabla.HasCheckConstraint("ck_reserva_presupuestaria_monto_positivo", "monto > 0");
        });

        builder.HasKey(reserva => reserva.Id).HasName("pk_reservas_presupuestarias");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(reserva => reserva.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(reserva => reserva.ReferenciaExternaId)
            .HasColumnName("referencia_externa_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(reserva => reserva.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(reserva => reserva.Monto)
            .ComoDinero("monto")
            .IsRequired();

        builder.Property(reserva => reserva.Moneda)
            .HasColumnName("moneda")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(reserva => reserva.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(reserva => reserva.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(reserva => reserva.MotivoLiberacion)
            .HasColumnName("motivo_liberacion")
            .HasMaxLength(500);

        builder.Property(reserva => reserva.FechaReserva)
            .HasColumnName("fecha_reserva")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(reserva => reserva.FechaResolucion)
            .HasColumnName("fecha_resolucion")
            .HasColumnType("timestamptz");

        builder.HasIndex(reserva => new { reserva.ProgramaBecaId, reserva.ReferenciaExternaId })
            .IsUnique()
            .HasDatabaseName("ux_reserva_presupuestaria_programa_referencia");

        builder.HasIndex(reserva => new { reserva.ProgramaBecaId, reserva.PeriodoAcademicoId, reserva.Estado })
            .HasDatabaseName("ix_reserva_presupuestaria_programa_periodo_estado");
    }
}
