using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Renovacion.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class SolicitudRenovacionConfiguracion
    : IEntityTypeConfiguration<SolicitudRenovacion>
{
    public void Configure(EntityTypeBuilder<SolicitudRenovacion> builder)
    {
        builder.ToTable("solicitud_renovacion", tabla =>
        {
            tabla.HasCheckConstraint("ck_solicitud_renovacion_estado",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoRenovacion>()})");
            tabla.HasCheckConstraint("ck_solicitud_renovacion_monto_reserva",
                "(reserva_presupuesto_id IS NULL AND monto_reservado_renovacion = 0) OR (reserva_presupuesto_id IS NOT NULL AND monto_reservado_renovacion > 0)");
            tabla.HasCheckConstraint("ck_solicitud_renovacion_periodos",
                "periodo_origen_id <> periodo_solicitado_id");
            tabla.HasCheckConstraint("ck_solicitud_renovacion_fechas",
                "fecha_limite_ventana_renovacion >= (fecha_solicitud AT TIME ZONE 'UTC')::date");
            tabla.HasCheckConstraint("ck_solicitud_renovacion_elegibilidad",
                "cumple_rendimiento_academico AND tiene_presupuesto_disponible");
        });

        builder.HasKey(solicitud => solicitud.Id)
            .HasName("pk_solicitud_renovacion");
        builder.Ignore(solicitud => solicitud.EventosDominio);
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);

        builder.Property(solicitud => solicitud.BecaId)
            .HasColumnName("beca_id").HasColumnType("uuid");
        builder.Property(solicitud => solicitud.PeriodoOrigenId)
            .HasColumnName("periodo_origen_id").HasColumnType("uuid");
        builder.Property(solicitud => solicitud.PeriodoSolicitadoId)
            .HasColumnName("periodo_solicitado_id").HasColumnType("uuid");
        builder.Property(solicitud => solicitud.FechaSolicitud)
            .HasColumnName("fecha_solicitud").HasColumnType("timestamptz");
        builder.Property(solicitud => solicitud.FechaLimiteVentanaRenovacion)
            .HasColumnName("fecha_limite_ventana_renovacion").HasColumnType("date");
        builder.Property(solicitud => solicitud.CumpleRendimientoAcademico)
            .HasColumnName("cumple_rendimiento_academico");
        builder.Property(solicitud => solicitud.TienePresupuestoDisponible)
            .HasColumnName("tiene_presupuesto_disponible");
        builder.Property(solicitud => solicitud.MontoReservadoRenovacion)
            .ConfigurarMonto("monto_reservado_renovacion");
        builder.Property(solicitud => solicitud.ReservaPresupuestoId)
            .HasColumnName("reserva_presupuesto_id").HasColumnType("uuid");
        builder.Property(solicitud => solicitud.Estado)
            .HasColumnName("estado").ComoCodigoEnum().HasMaxLength(30);
        builder.Property(solicitud => solicitud.MotivoDecision)
            .HasColumnName("motivo_decision").HasMaxLength(2000);
        builder.Property(solicitud => solicitud.EvaluadoPorCoordinadorId)
            .HasColumnName("evaluado_por_coordinador_id").HasColumnType("uuid");
        builder.Property(solicitud => solicitud.FechaDecision)
            .HasColumnName("fecha_decision").HasColumnType("timestamptz");
        builder.Property(solicitud => solicitud.ReferenciaJobVencimientoId)
            .HasColumnName("referencia_job_vencimiento_id").HasMaxLength(200);

        builder.HasOne<Beca>().WithMany()
            .HasForeignKey(solicitud => solicitud.BecaId)
            .HasConstraintName("fk_solicitud_renovacion_beca")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(solicitud => new { solicitud.BecaId, solicitud.PeriodoSolicitadoId })
            .IsUnique()
            .HasDatabaseName("ux_solicitud_renovacion_beca_periodo");
    }
}
