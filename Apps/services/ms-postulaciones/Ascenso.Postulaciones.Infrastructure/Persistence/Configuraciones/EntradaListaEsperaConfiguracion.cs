using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el agregado <see cref="EntradaListaEspera"/>.
/// </summary>
internal sealed class EntradaListaEsperaConfiguracion : IEntityTypeConfiguration<EntradaListaEspera>
{
    public void Configure(EntityTypeBuilder<EntradaListaEspera> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var estadosSql = ConfiguracionPersistencia.ValoresSql<EstadoListaEspera>();

        builder.ToTable("entradas_lista_espera", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_entradas_lista_espera_estado_valido",
                $"estado IN ({estadosSql})");
            tabla.HasCheckConstraint(
                "ck_entradas_lista_espera_posicion_positiva",
                "posicion_orden >= 1");
            tabla.HasCheckConstraint(
                "ck_entradas_lista_espera_monto_positivo",
                "monto_estimado_beca_snapshot > 0");
            tabla.HasCheckConstraint(
                "ck_entradas_lista_espera_estado_coherente",
                $"(estado = 'EN_ESPERA' AND fecha_elegibilidad IS NULL AND fecha_promocion IS NULL AND promovido_por_usuario_id IS NULL AND motivo_promocion IS NULL) OR (estado = 'ELEGIBLE_POR_VACANTE' AND fecha_elegibilidad IS NOT NULL AND fecha_promocion IS NULL AND promovido_por_usuario_id IS NULL AND motivo_promocion IS NULL) OR (estado = 'PROMOVIDA' AND fecha_promocion IS NOT NULL AND promovido_por_usuario_id IS NOT NULL AND (motivo_promocion IS NULL OR length(btrim(motivo_promocion)) > 0)) OR estado NOT IN ({estadosSql})");
        });

        builder.HasKey(entrada => entrada.Id)
            .HasName("pk_entradas_lista_espera");

        builder.Ignore(entrada => entrada.EventosDominio);
        builder.Ignore(entrada => entrada.EstaPromovida);
        builder.Ignore(entrada => entrada.EstaPendiente);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(entrada => entrada.ConvocatoriaId)
            .HasColumnName("convocatoria_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(entrada => entrada.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(entrada => entrada.PostulacionId)
            .HasColumnName("postulacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(entrada => entrada.PosicionOrden)
            .HasColumnName("posicion_orden")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(entrada => entrada.MontoEstimadoBecaSnapshot)
            .HasColumnName("monto_estimado_beca_snapshot")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(entrada => entrada.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(entrada => entrada.FechaIngreso)
            .HasColumnName("fecha_ingreso")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(entrada => entrada.FechaElegibilidad)
            .HasColumnName("fecha_elegibilidad")
            .HasColumnType("timestamptz");

        builder.Property(entrada => entrada.FechaPromocion)
            .HasColumnName("fecha_promocion")
            .HasColumnType("timestamptz");

        builder.Property(entrada => entrada.PromovidoPorUsuarioId)
            .HasColumnName("promovido_por_usuario_id")
            .HasColumnType("uuid");

        builder.Property(entrada => entrada.MotivoPromocion)
            .HasColumnName("motivo_promocion")
            .HasMaxLength(EntradaListaEspera.LongitudMaximaMotivoPromocion);

        builder.HasIndex(entrada => entrada.PostulacionId)
            .IsUnique()
            .HasDatabaseName(TraductorExcepcionesPersistencia.IndiceEntradaListaEsperaPostulacionUnico);

        builder.HasIndex(entrada => new
        {
            entrada.ConvocatoriaId,
            entrada.PosicionOrden
        })
            .IsUnique()
            .HasDatabaseName(TraductorExcepcionesPersistencia.IndiceEntradaListaEsperaConvocatoriaPosicionUnico);

        builder.HasIndex(entrada => new
        {
            entrada.ConvocatoriaId,
            entrada.Estado,
            entrada.PosicionOrden
        })
            .HasDatabaseName("ix_entradas_lista_espera_convocatoria_estado_posicion");
    }
}
