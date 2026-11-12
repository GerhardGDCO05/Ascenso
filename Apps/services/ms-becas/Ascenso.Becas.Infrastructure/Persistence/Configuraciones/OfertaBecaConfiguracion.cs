using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class OfertaBecaConfiguracion : IEntityTypeConfiguration<OfertaBeca>
{
    public void Configure(EntityTypeBuilder<OfertaBeca> builder)
    {
        builder.ToTable("oferta_beca", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_oferta_beca_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoOferta>()})");
            tabla.HasCheckConstraint(
                "ck_oferta_beca_montos_no_negativos",
                "monto_oferta_por_periodo >= 0 AND monto_total_estimado_duracion >= 0");
            tabla.HasCheckConstraint(
                "ck_oferta_beca_duracion_valida",
                "duracion_periodos > 0");
            tabla.HasCheckConstraint(
                "ck_oferta_beca_fechas_coherentes",
                """
                fecha_vencimiento_aceptacion > fecha_emision
                AND (fecha_respuesta IS NULL OR fecha_respuesta >= fecha_emision)
                """);
            tabla.HasCheckConstraint(
                "ck_oferta_beca_condiciones_validas",
                $"""
                condiciones_promedio_minimo_renovacion >= 0
                AND condiciones_promedio_minimo_renovacion <= 20
                AND condiciones_materias_minimas_aprobadas >= 0
                AND condiciones_maximo_materias_reprobadas >= 0
                AND condiciones_horas_servicio_por_periodo >= 0
                AND condiciones_consecuencia_incumplimiento IN ({ConfiguracionPersistencia.ValoresSql<ConsecuenciaPermanencia>()})
                """);
        });

        builder.HasKey(oferta => oferta.Id).HasName("pk_oferta_beca");

        ConfiguracionPersistencia.ConfigurarAuditoria(builder);

        builder.Property(oferta => oferta.BecaId)
            .HasColumnName("beca_id")
            .HasColumnType("uuid");

        builder.Property(oferta => oferta.PostulacionId)
            .HasColumnName("postulacion_id")
            .HasColumnType("uuid");

        builder.Property(oferta => oferta.ReservaPresupuestoId)
            .HasColumnName("reserva_presupuesto_id")
            .HasColumnType("uuid");

        builder.Property(oferta => oferta.MontoOfertaPorPeriodo)
            .ConfigurarMonto("monto_oferta_por_periodo");

        builder.Property(oferta => oferta.MontoTotalEstimadoDuracion)
            .ConfigurarMonto("monto_total_estimado_duracion");

        builder.Property(oferta => oferta.DuracionPeriodos)
            .HasColumnName("duracion_periodos");

        builder.OwnsOne(oferta => oferta.CondicionesSnapshot, condiciones =>
        {
            condiciones.Property(valor => valor.PromedioMinimoRenovacion)
                .ConfigurarPorcentaje("condiciones_promedio_minimo_renovacion");

            condiciones.Property(valor => valor.MateriasMinimasAprobadas)
                .HasColumnName("condiciones_materias_minimas_aprobadas");

            condiciones.Property(valor => valor.MaximoMateriasReprobadas)
                .HasColumnName("condiciones_maximo_materias_reprobadas");

            condiciones.Property(valor => valor.HorasServicioPorPeriodo)
                .HasColumnName("condiciones_horas_servicio_por_periodo");

            condiciones.Property(valor => valor.ConsecuenciaIncumplimiento)
                .HasColumnName("condiciones_consecuencia_incumplimiento")
                .ComoCodigoEnum()
                .HasMaxLength(30);
        });

        builder.Property(oferta => oferta.UrlCartaPdfFirebase)
            .HasColumnName("url_carta_pdf_firebase")
            .HasMaxLength(2048);

        builder.Property(oferta => oferta.HashCartaSha256)
            .HasColumnName("hash_carta_sha256")
            .HasMaxLength(64)
            .IsFixedLength();

        builder.Property(oferta => oferta.FechaEmision)
            .HasColumnName("fecha_emision")
            .HasColumnType("timestamptz");

        builder.Property(oferta => oferta.FechaVencimientoAceptacion)
            .HasColumnName("fecha_vencimiento_aceptacion")
            .HasColumnType("timestamptz");

        builder.Property(oferta => oferta.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(30);

        builder.Property(oferta => oferta.MotivoRechazoOAnulacion)
            .HasColumnName("motivo_rechazo_o_anulacion")
            .HasMaxLength(1000);

        builder.Property(oferta => oferta.FechaRespuesta)
            .HasColumnName("fecha_respuesta")
            .HasColumnType("timestamptz");

        builder.Property(oferta => oferta.ReferenciaJobExpiracionHangfireId)
            .HasColumnName("referencia_job_expiracion_hangfire_id")
            .HasMaxLength(100);

        builder.Property(oferta => oferta.EmitidaPorUsuarioId)
            .HasColumnName("emitida_por_usuario_id")
            .HasColumnType("uuid");

        builder.Property(oferta => oferta.AutorizacionOtpEmisionId)
            .HasColumnName("autorizacion_otp_emision_id")
            .HasColumnType("uuid");

        builder.Property(oferta => oferta.AutorizacionOtpAceptacionId)
            .HasColumnName("autorizacion_otp_aceptacion_id")
            .HasColumnType("uuid");

        builder.HasIndex(oferta => oferta.BecaId)
            .IsUnique()
            .HasDatabaseName("ux_oferta_beca_beca");
    }
}
