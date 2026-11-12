using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class ProgramaBecaRefConfiguracion : IEntityTypeConfiguration<ProgramaBecaRef>
{
    public void Configure(EntityTypeBuilder<ProgramaBecaRef> builder)
    {
        builder.ToTable("programa_beca_ref", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_programa_beca_ref_cobertura_modalidad_valida",
                $"politica_cobertura_modalidad IN ({ConfiguracionPersistencia.ValoresSql<TipoCobertura>()})");
            tabla.HasCheckConstraint(
                "ck_programa_beca_ref_cobertura_porcentaje_valido",
                "politica_cobertura_porcentaje >= 0 AND politica_cobertura_porcentaje <= 100");
            tabla.HasCheckConstraint(
                "ck_programa_beca_ref_montos_no_negativos",
                "politica_cobertura_monto_fijo_periodo >= 0");
            tabla.HasCheckConstraint(
                "ck_programa_beca_ref_condiciones_validas",
                $"""
                condiciones_permanencia_promedio_minimo_renovacion >= 0
                AND condiciones_permanencia_promedio_minimo_renovacion <= 20
                AND condiciones_permanencia_materias_minimas_aprobadas >= 0
                AND condiciones_permanencia_maximo_materias_reprobadas >= 0
                AND condiciones_permanencia_horas_servicio_por_periodo >= 0
                AND condiciones_permanencia_consecuencia_incumplimiento IN ({ConfiguracionPersistencia.ValoresSql<ConsecuenciaPermanencia>()})
                """);
            tabla.HasCheckConstraint(
                "ck_programa_beca_ref_duracion_valida",
                "duracion_periodos > 0");
        });

        builder.HasKey(programa => programa.ProgramaBecaId).HasName("pk_programa_beca_ref");

        builder.Property(programa => programa.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid");

        builder.Property(programa => programa.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50);

        builder.Property(programa => programa.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200);

        builder.OwnsOne(programa => programa.PoliticaCobertura, cobertura =>
        {
            cobertura.Property(valor => valor.Modalidad)
                .HasColumnName("politica_cobertura_modalidad")
                .ComoCodigoEnum()
                .HasMaxLength(20);

            cobertura.Property(valor => valor.PorcentajeCobertura)
                .ConfigurarPorcentaje("politica_cobertura_porcentaje");

            cobertura.Property(valor => valor.MontoFijoPeriodo)
                .ConfigurarMonto("politica_cobertura_monto_fijo_periodo");

            cobertura.Property(valor => valor.Moneda)
                .HasColumnName("politica_cobertura_moneda")
                .HasMaxLength(3)
                .IsFixedLength();
        });

        builder.OwnsOne(programa => programa.CondicionesPermanencia, condiciones =>
        {
            condiciones.Property(valor => valor.PromedioMinimoRenovacion)
                .ConfigurarPorcentaje("condiciones_permanencia_promedio_minimo_renovacion");

            condiciones.Property(valor => valor.MateriasMinimasAprobadas)
                .HasColumnName("condiciones_permanencia_materias_minimas_aprobadas");

            condiciones.Property(valor => valor.MaximoMateriasReprobadas)
                .HasColumnName("condiciones_permanencia_maximo_materias_reprobadas");

            condiciones.Property(valor => valor.HorasServicioPorPeriodo)
                .HasColumnName("condiciones_permanencia_horas_servicio_por_periodo");

            condiciones.Property(valor => valor.ConsecuenciaIncumplimiento)
                .HasColumnName("condiciones_permanencia_consecuencia_incumplimiento")
                .ComoCodigoEnum()
                .HasMaxLength(30);
        });

        builder.Property(programa => programa.PermiteAcumulacion)
            .HasColumnName("permite_acumulacion");

        builder.Property(programa => programa.RequiereCompromisoFirmado)
            .HasColumnName("requiere_compromiso_firmado");

        builder.Property(programa => programa.DuracionPeriodos)
            .HasColumnName("duracion_periodos");

        builder.Property(programa => programa.Estado)
            .HasColumnName("estado")
            .HasMaxLength(30);

        builder.Property(programa => programa.UltimoEventoId)
            .HasColumnName("ultimo_evento_id")
            .HasColumnType("uuid");

        builder.Property(programa => programa.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz");

        builder.Property(programa => programa.FechaUltimaActualizacion)
            .HasColumnName("fecha_ultima_actualizacion")
            .HasColumnType("timestamptz");
    }
}
