using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class AporteDonanteConfiguracion : IEntityTypeConfiguration<AporteDonante>
{
    public void Configure(EntityTypeBuilder<AporteDonante> builder)
    {
        builder.ToTable("aportes_donante", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_aporte_donante_estado_valido",
                $"estado_aporte IN ({ConfiguracionPersistencia.ValoresSql<EstadoAporte>()})");
            tabla.HasCheckConstraint(
                "ck_aporte_donante_monto_positivo",
                "monto_aporte > 0");
        });

        builder.HasKey(aporte => aporte.Id).HasName("pk_aportes_donante");

        builder.Ignore(aporte => aporte.RegistradoPorUsuarioId);

        builder.Property(aporte => aporte.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(aporte => aporte.DonanteId)
            .HasColumnName("donante_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(aporte => aporte.MontoAporte)
            .ComoDinero("monto_aporte");

        builder.Property(aporte => aporte.Moneda)
            .HasColumnName("moneda")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(aporte => aporte.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(aporte => aporte.FechaAporte)
            .HasColumnName("fecha_aporte")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(aporte => aporte.BancoEmisor)
            .HasColumnName("banco_emisor")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(aporte => aporte.NumeroReferenciaBancaria)
            .HasColumnName("numero_referencia_bancaria")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(aporte => aporte.UrlComprobanteFirebase)
            .HasColumnName("url_comprobante_firebase")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(aporte => aporte.EstadoAporte)
            .HasColumnName("estado_aporte")
            .ComoCodigoEnum()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(aporte => aporte.Observaciones)
            .HasColumnName("observaciones")
            .HasMaxLength(1000);

        builder.Property(aporte => aporte.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(aporte => aporte.UsuarioAutorId)
            .HasColumnName("usuario_autor_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(aporte => aporte.CorrelacionId)
            .HasColumnName("correlacion_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(aporte => aporte.NumeroReferenciaBancaria)
            .IsUnique()
            .HasDatabaseName("ux_aporte_donante_referencia_bancaria");

        builder.HasIndex(aporte => new { aporte.DonanteId, aporte.PeriodoAcademicoId, aporte.FechaAporte })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_aporte_donante_donante_periodo");
    }
}

