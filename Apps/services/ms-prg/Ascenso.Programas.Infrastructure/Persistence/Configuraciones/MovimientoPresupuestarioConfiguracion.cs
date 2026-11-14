using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class MovimientoPresupuestarioConfiguracion : IEntityTypeConfiguration<MovimientoPresupuestario>
{
    public void Configure(EntityTypeBuilder<MovimientoPresupuestario> builder)
    {
        builder.ToTable("movimientos_presupuestarios", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_movimiento_presupuestario_tipo_valido",
                $"tipo_movimiento IN ({ConfiguracionPersistencia.ValoresSql<TipoMovimientoPresupuestario>()})");
            tabla.HasCheckConstraint("ck_movimiento_presupuestario_monto_no_negativo", "monto >= 0");
        });

        builder.HasKey(movimiento => movimiento.Id).HasName("pk_movimientos_presupuestarios");

        builder.Property(movimiento => movimiento.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(movimiento => movimiento.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(movimiento => movimiento.TipoMovimiento)
            .HasColumnName("tipo_movimiento")
            .ComoCodigoEnum()
            .HasMaxLength(35)
            .IsRequired();

        builder.Property(movimiento => movimiento.Monto)
            .ComoDinero("monto")
            .IsRequired();

        builder.Property(movimiento => movimiento.Moneda)
            .HasColumnName("moneda")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(movimiento => movimiento.ReferenciaExternaId)
            .HasColumnName("referencia_externa_id")
            .HasColumnType("uuid");

        builder.Property(movimiento => movimiento.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(movimiento => movimiento.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(movimiento => movimiento.FechaMovimiento)
            .HasColumnName("fecha_movimiento")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(movimiento => movimiento.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(movimiento => movimiento.UsuarioAutorId)
            .HasColumnName("usuario_autor_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(movimiento => movimiento.CorrelacionId)
            .HasColumnName("correlacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(movimiento => new { movimiento.ProgramaBecaId, movimiento.FechaMovimiento })
            .IsDescending(false, true)
            .HasDatabaseName("ix_movimiento_presupuestario_programa_fecha");

        builder.HasIndex(movimiento => new { movimiento.ProgramaBecaId, movimiento.PeriodoAcademicoId })
            .HasDatabaseName("ix_movimiento_presupuestario_programa_periodo");
    }
}
