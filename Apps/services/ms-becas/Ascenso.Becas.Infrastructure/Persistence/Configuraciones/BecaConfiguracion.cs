using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class BecaConfiguracion : IEntityTypeConfiguration<Beca>
{
    public void Configure(EntityTypeBuilder<Beca> builder)
    {
        builder.ToTable("beca", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_beca_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoBeca>()})");
            tabla.HasCheckConstraint(
                "ck_beca_tipo_cierre_valido",
                $"tipo_cierre IS NULL OR tipo_cierre IN ({ConfiguracionPersistencia.ValoresSql<TipoCierreBeca>()})");
            tabla.HasCheckConstraint(
                "ck_beca_cobertura_modalidad_valida",
                $"cobertura_modalidad IN ({ConfiguracionPersistencia.ValoresSql<TipoCobertura>()})");
            tabla.HasCheckConstraint(
                "ck_beca_cobertura_porcentaje_valido",
                "cobertura_porcentaje >= 0 AND cobertura_porcentaje <= 100");
            tabla.HasCheckConstraint(
                "ck_beca_montos_no_negativos",
                """
                monto_comprometido_periodo_actual >= 0
                AND monto_desembolsado_periodo_actual >= 0
                AND cobertura_monto_fijo_periodo >= 0
                """);
            tabla.HasCheckConstraint(
                "ck_beca_periodos_validos",
                """
                periodos_cursados >= 0
                AND duracion_total_periodos > 0
                AND periodos_cursados <= duracion_total_periodos
                """);
            tabla.HasCheckConstraint(
                "ck_beca_fechas_coherentes",
                """
                (fecha_activacion IS NULL OR fecha_activacion >= fecha_otorgamiento)
                AND (fecha_conclusion_o_revocacion IS NULL OR fecha_conclusion_o_revocacion >= fecha_otorgamiento)
                """);
        });

        builder.HasKey(beca => beca.Id).HasName("pk_beca");

        builder.Ignore(beca => beca.EventosDominio);

        ConfiguracionPersistencia.ConfigurarAuditoria(builder);

        builder.Property(beca => beca.CadenaBecaId)
            .HasColumnName("cadena_beca_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.BecaAnteriorId)
            .HasColumnName("beca_anterior_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.CodigoBeca)
            .HasColumnName("codigo_beca")
            .HasMaxLength(20);

        builder.Property(beca => beca.PostulanteEstudianteId)
            .HasColumnName("postulante_estudiante_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.NumeroDocumentoEstudiante)
            .HasColumnName("numero_documento_estudiante")
            .HasMaxLength(30);

        builder.Property(beca => beca.PostulacionOrigenId)
            .HasColumnName("postulacion_origen_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.ConvocatoriaOrigenId)
            .HasColumnName("convocatoria_origen_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.PeriodoOtorgamientoId)
            .HasColumnName("periodo_otorgamiento_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.PeriodoVigenteActualId)
            .HasColumnName("periodo_vigente_actual_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.PeriodosCursados)
            .HasColumnName("periodos_cursados");

        builder.Property(beca => beca.DuracionTotalPeriodos)
            .HasColumnName("duracion_total_periodos");

        builder.OwnsOne(beca => beca.CoberturaAplicada, cobertura =>
        {
            cobertura.Property(valor => valor.Modalidad)
                .HasColumnName("cobertura_modalidad")
                .ComoCodigoEnum()
                .HasMaxLength(20);

            cobertura.Property(valor => valor.PorcentajeCobertura)
                .ConfigurarPorcentaje("cobertura_porcentaje");

            cobertura.Property(valor => valor.MontoFijoPeriodo)
                .ConfigurarMonto("cobertura_monto_fijo_periodo");

            cobertura.Property(valor => valor.Moneda)
                .HasColumnName("cobertura_moneda")
                .HasMaxLength(3)
                .IsFixedLength();
        });

        builder.Property(beca => beca.CostoMatriculaReferenciaSnapshotId)
            .HasColumnName("costo_matricula_referencia_snapshot_id")
            .HasColumnType("uuid");

        builder.Property(beca => beca.MontoComprometidoPeriodoActual)
            .ConfigurarMonto("monto_comprometido_periodo_actual");

        builder.Property(beca => beca.MontoDesembolsadoPeriodoActual)
            .ConfigurarMonto("monto_desembolsado_periodo_actual");

        builder.Property(beca => beca.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(30);

        builder.Property(beca => beca.TipoCierre)
            .HasColumnName("tipo_cierre")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(beca => beca.PermiteAcumulacion)
            .HasColumnName("permite_acumulacion");

        builder.Property(beca => beca.RequiereCompromisoFirmado)
            .HasColumnName("requiere_compromiso_firmado");

        builder.Property(beca => beca.CompromisoFirmadoAprobado)
            .HasColumnName("compromiso_firmado_aprobado");

        builder.Property(beca => beca.HabilitadaParaDesembolso)
            .HasColumnName("habilitada_para_desembolso");

        builder.Property(beca => beca.TieneRenovacionEnCurso)
            .HasColumnName("tiene_renovacion_en_curso");

        builder.Property(beca => beca.FechaOtorgamiento)
            .HasColumnName("fecha_otorgamiento")
            .HasColumnType("date");

        builder.Property(beca => beca.FechaActivacion)
            .HasColumnName("fecha_activacion")
            .HasColumnType("date");

        builder.Property(beca => beca.FechaConclusionORevocacion)
            .HasColumnName("fecha_conclusion_o_revocacion")
            .HasColumnType("date");

        builder.HasOne(beca => beca.Oferta)
            .WithOne()
            .HasForeignKey<OfertaBeca>(oferta => oferta.BecaId)
            .HasConstraintName("fk_oferta_beca_beca")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(beca => beca.Compromiso)
            .WithOne()
            .HasForeignKey<CompromisoBecario>(compromiso => compromiso.BecaId)
            .HasConstraintName("fk_compromiso_becario_beca")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(beca => beca.Historial)
            .WithOne()
            .HasForeignKey(historial => historial.BecaId)
            .HasConstraintName("fk_historial_estado_beca_beca")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(beca => beca.Historial)
            .HasField("_historial")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(beca => beca.CodigoBeca)
            .IsUnique()
            .HasDatabaseName("ux_beca_codigo");

        builder.HasIndex(beca => new { beca.CadenaBecaId, beca.PeriodoVigenteActualId })
            .IsUnique()
            .HasDatabaseName("ux_beca_cadena_periodo");

        builder.HasIndex(beca => beca.PostulanteEstudianteId)
            .IsUnique()
            .HasDatabaseName("ux_beca_estudiante_cobertura_total_activa")
            .HasFilter("estado = 'ACTIVA' AND cobertura_modalidad = 'TOTAL'");

        builder.HasIndex(beca => new { beca.PostulanteEstudianteId, beca.PeriodoVigenteActualId })
            .IsUnique()
            .HasDatabaseName("ux_beca_estudiante_periodo_no_acumulable_activa")
            .HasFilter("estado = 'ACTIVA' AND permite_acumulacion = false");
    }
}
