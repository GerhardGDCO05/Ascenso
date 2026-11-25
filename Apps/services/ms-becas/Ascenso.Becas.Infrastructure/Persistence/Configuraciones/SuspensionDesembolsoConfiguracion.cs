using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class SuspensionDesembolsoConfiguracion : IEntityTypeConfiguration<SuspensionDesembolso>
{
    public void Configure(EntityTypeBuilder<SuspensionDesembolso> builder)
    {
        builder.ToTable("suspension_desembolso", tabla =>
        {
            tabla.HasCheckConstraint("ck_suspension_tipo",
                $"tipo_suspension IN ({ConfiguracionPersistencia.ValoresSql<TipoSuspensionBeca>()})");
            tabla.HasCheckConstraint("ck_suspension_estado",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoSuspension>()})");
            tabla.HasCheckConstraint("ck_suspension_revision",
                "(es_indefinida AND fecha_revision_programada IS NULL) OR " +
                "(NOT es_indefinida AND fecha_revision_programada IS NOT NULL AND fecha_revision_programada >= fecha_inicio)");
        });
        builder.HasKey(item => item.Id).HasName("pk_suspension_desembolso");
        builder.Property(item => item.Id).ValueGeneratedNever();
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);
        builder.Property(item => item.BecaId).HasColumnName("beca_id").HasColumnType("uuid");
        builder.Property(item => item.TipoSuspension).HasColumnName("tipo_suspension")
            .ComoCodigoEnum().HasMaxLength(40);
        builder.Property(item => item.MotivoSuspension).HasColumnName("motivo_suspension")
            .HasMaxLength(1000);
        builder.Property(item => item.RequisitosParaReanudar).HasColumnName("requisitos_para_reanudar")
            .HasMaxLength(1000);
        builder.Property(item => item.FechaInicio).HasColumnName("fecha_inicio");
        builder.Property(item => item.FechaRevisionProgramada).HasColumnName("fecha_revision_programada");
        builder.Property(item => item.EsIndefinida).HasColumnName("es_indefinida");
        builder.Property(item => item.Estado).HasColumnName("estado").ComoCodigoEnum().HasMaxLength(20);
        builder.Property(item => item.RegistradaPorCoordinadorId)
            .HasColumnName("registrada_por_coordinador_id").HasColumnType("uuid");
        builder.Property(item => item.LevantadaPorCoordinadorId)
            .HasColumnName("levantada_por_coordinador_id").HasColumnType("uuid");
        builder.Property(item => item.FechaResolucion).HasColumnName("fecha_resolucion");
        builder.HasIndex(item => new { item.BecaId, item.Estado })
            .HasDatabaseName("ix_suspension_desembolso_beca_estado");
    }
}
