using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.SeguimientoAcademico.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class EvaluacionPermanenciaConfiguracion : IEntityTypeConfiguration<EvaluacionPermanencia>
{
    public void Configure(EntityTypeBuilder<EvaluacionPermanencia> builder)
    {
        builder.ToTable("evaluacion_permanencia", tabla =>
        {
            tabla.HasCheckConstraint("ck_evaluacion_permanencia_recomendada",
                $"consecuencia_recomendada IS NULL OR consecuencia_recomendada IN ({ConfiguracionPersistencia.ValoresSql<ConsecuenciaPermanencia>()})");
            tabla.HasCheckConstraint("ck_evaluacion_permanencia_aplicada",
                $"consecuencia_aplicada IS NULL OR consecuencia_aplicada IN ({ConfiguracionPersistencia.ValoresSql<ConsecuenciaPermanencia>()})");
            tabla.HasCheckConstraint("ck_evaluacion_permanencia_estado",
                $"estado_confirmacion IN ({ConfiguracionPersistencia.ValoresSql<EstadoConsecuencia>()})");
        });
        builder.HasKey(evaluacion => evaluacion.Id).HasName("pk_evaluacion_permanencia");
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);

        builder.Property(evaluacion => evaluacion.ReporteAcademicoId)
            .HasColumnName("reporte_academico_id").HasColumnType("uuid");
        builder.Property(evaluacion => evaluacion.BecaId)
            .HasColumnName("beca_id").HasColumnType("uuid");
        builder.Property(evaluacion => evaluacion.FechaEvaluacion)
            .HasColumnName("fecha_evaluacion").HasColumnType("timestamptz");
        builder.Property(evaluacion => evaluacion.CumpleCondiciones)
            .HasColumnName("cumple_condiciones");
        builder.Property(evaluacion => evaluacion.ConsecuenciaRecomendada)
            .HasColumnName("consecuencia_recomendada")
            .ComoCodigoEnumOpcional().HasMaxLength(30);
        builder.Property(evaluacion => evaluacion.ConsecuenciaAplicada)
            .HasColumnName("consecuencia_aplicada")
            .ComoCodigoEnumOpcional().HasMaxLength(30);
        builder.Property(evaluacion => evaluacion.EstadoConfirmacion)
            .HasColumnName("estado_confirmacion")
            .ComoCodigoEnum().HasMaxLength(30);
        builder.Property(evaluacion => evaluacion.MotivoDecision)
            .HasColumnName("motivo_decision").HasMaxLength(2000);
        builder.Property(evaluacion => evaluacion.SiguientePasoBecario)
            .HasColumnName("siguiente_paso_becario").HasMaxLength(2000);
        builder.Property(evaluacion => evaluacion.EvaluadoPorCoordinadorId)
            .HasColumnName("evaluado_por_coordinador_id").HasColumnType("uuid");
        builder.HasIndex(evaluacion => evaluacion.ReporteAcademicoId)
            .IsUnique().HasDatabaseName("ux_evaluacion_permanencia_reporte");

        builder.OwnsMany(evaluacion => evaluacion.DetalleCondiciones, detalle =>
        {
            detalle.ToTable("condicion_permanencia_evaluada", tabla =>
                tabla.HasCheckConstraint("ck_condicion_permanencia_evaluada_tipo",
                    $"condicion IN ({ConfiguracionPersistencia.ValoresSql<TipoCondicionPermanencia>()})"));
            detalle.WithOwner().HasForeignKey("evaluacion_permanencia_id");
            detalle.Property<Guid>("id").HasColumnName("id").HasColumnType("uuid");
            detalle.HasKey("id").HasName("pk_condicion_permanencia_evaluada");
            detalle.Property(valor => valor.Condicion)
                .HasColumnName("condicion").ComoCodigoEnum().HasMaxLength(40);
            detalle.Property(valor => valor.ValorRequerido)
                .HasColumnName("valor_requerido").HasMaxLength(40);
            detalle.Property(valor => valor.ValorObtenido)
                .HasColumnName("valor_obtenido").HasMaxLength(40);
            detalle.Property(valor => valor.Cumple).HasColumnName("cumple");
            detalle.HasIndex("evaluacion_permanencia_id", nameof(Ascenso.Becas.Domain.SeguimientoAcademico.ValueObjects.CondicionPermanenciaEvaluada.Condicion))
                .IsUnique().HasDatabaseName("ux_condicion_permanencia_evaluada_tipo");
        });
        builder.Navigation(evaluacion => evaluacion.DetalleCondiciones)
            .HasField("_detalleCondiciones")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
