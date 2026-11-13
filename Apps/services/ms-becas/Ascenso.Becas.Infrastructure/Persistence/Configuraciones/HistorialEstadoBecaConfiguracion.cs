using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class HistorialEstadoBecaConfiguracion : IEntityTypeConfiguration<HistorialEstadoBeca>
{
    public void Configure(EntityTypeBuilder<HistorialEstadoBeca> builder)
    {
        builder.ToTable("historial_estado_beca", tabla =>
        {
            tabla.HasTrigger("trg_historial_estado_beca_inmutable");

            tabla.HasCheckConstraint(
                "ck_historial_estado_beca_tipo_entidad_valido",
                $"tipo_entidad IN ({ConfiguracionPersistencia.ValoresSql<TipoEntidadHistorial>()})");
            tabla.HasCheckConstraint(
                "ck_historial_estado_beca_estado_anterior_valido",
                $"""
                (estado_beca_anterior IS NULL OR estado_beca_anterior IN ({ConfiguracionPersistencia.ValoresSql<EstadoBeca>()}))
                AND (estado_oferta_anterior IS NULL OR estado_oferta_anterior IN ({ConfiguracionPersistencia.ValoresSql<EstadoOferta>()}))
                AND (estado_compromiso_anterior IS NULL OR estado_compromiso_anterior IN ({ConfiguracionPersistencia.ValoresSql<EstadoCompromiso>()}))
                """);
            tabla.HasCheckConstraint(
                "ck_historial_estado_beca_estado_nuevo_valido",
                $"""
                (estado_beca_nuevo IS NULL OR estado_beca_nuevo IN ({ConfiguracionPersistencia.ValoresSql<EstadoBeca>()}))
                AND (estado_oferta_nuevo IS NULL OR estado_oferta_nuevo IN ({ConfiguracionPersistencia.ValoresSql<EstadoOferta>()}))
                AND (estado_compromiso_nuevo IS NULL OR estado_compromiso_nuevo IN ({ConfiguracionPersistencia.ValoresSql<EstadoCompromiso>()}))
                """);
        });

        builder.HasKey(historial => historial.Id).HasName("pk_historial_estado_beca");

        builder.HasIndex(historial => historial.BecaId)
            .HasDatabaseName("ix_historial_estado_beca_beca_id");

        builder.Property(historial => historial.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(historial => historial.BecaId)
            .HasColumnName("beca_id")
            .HasColumnType("uuid");

        builder.Property(historial => historial.OfertaBecaId)
            .HasColumnName("oferta_beca_id")
            .HasColumnType("uuid");

        builder.Property(historial => historial.CompromisoBecarioId)
            .HasColumnName("compromiso_becario_id")
            .HasColumnType("uuid");

        builder.Property(historial => historial.TipoEntidad)
            .HasColumnName("tipo_entidad")
            .ComoCodigoEnum()
            .HasMaxLength(30);

        builder.Property(historial => historial.EstadoBecaAnterior)
            .HasColumnName("estado_beca_anterior")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(historial => historial.EstadoBecaNuevo)
            .HasColumnName("estado_beca_nuevo")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(historial => historial.EstadoOfertaAnterior)
            .HasColumnName("estado_oferta_anterior")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(historial => historial.EstadoOfertaNuevo)
            .HasColumnName("estado_oferta_nuevo")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(historial => historial.EstadoCompromisoAnterior)
            .HasColumnName("estado_compromiso_anterior")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(historial => historial.EstadoCompromisoNuevo)
            .HasColumnName("estado_compromiso_nuevo")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(historial => historial.FechaCambio)
            .HasColumnName("fecha_cambio")
            .HasColumnType("timestamptz");

        builder.Property(historial => historial.Motivo)
            .HasColumnName("motivo")
            .HasMaxLength(500);

        builder.Property(historial => historial.JustificacionDetallada)
            .HasColumnName("justificacion_detallada")
            .HasMaxLength(2000);

        builder.Property(historial => historial.AutorizacionOtpId)
            .HasColumnName("autorizacion_otp_id")
            .HasColumnType("uuid");

        builder.Property(historial => historial.EstadoTerminalIrreversible)
            .HasColumnName("estado_terminal_irreversible");

        builder.Property(historial => historial.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasColumnType("timestamptz");

        builder.Property(historial => historial.UsuarioAutorId)
            .HasColumnName("usuario_autor_id")
            .HasColumnType("uuid");

        builder.Property(historial => historial.CorrelacionId)
            .HasColumnName("correlacion_id")
            .HasColumnType("uuid");
    }
}
