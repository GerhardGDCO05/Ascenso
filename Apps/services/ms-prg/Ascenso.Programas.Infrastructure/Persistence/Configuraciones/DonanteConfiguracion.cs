using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class DonanteConfiguracion : IEntityTypeConfiguration<Donante>
{
    public void Configure(EntityTypeBuilder<Donante> builder)
    {
        builder.ToTable("donantes", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_donante_tipo_valido",
                $"tipo_donante IN ({ConfiguracionPersistencia.ValoresSql<TipoDonante>()})");
            tabla.HasCheckConstraint(
                "ck_donante_estado_valido",
                $"estado_donante IN ({ConfiguracionPersistencia.ValoresSql<EstadoDonante>()})");
            tabla.HasCheckConstraint(
                "ck_donante_saldos_no_negativos",
                """
                saldo_total_historico >= 0
                AND saldo_disponible_asignacion >= 0
                AND saldo_disponible_asignacion <= saldo_total_historico
                """);
        });

        builder.HasKey(donante => donante.Id).HasName("pk_donantes");

        builder.Ignore(donante => donante.EventosDominio);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(donante => donante.TipoDonante)
            .HasColumnName("tipo_donante")
            .ComoCodigoEnum()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(donante => donante.RazonSocialNombre)
            .HasColumnName("razon_social_nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(donante => donante.IdentificacionFiscal)
            .HasColumnName("identificacion_fiscal")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(donante => donante.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(donante => donante.Telefono)
            .HasColumnName("telefono")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(donante => donante.DireccionFiscal)
            .HasColumnName("direccion_fiscal")
            .HasMaxLength(400)
            .IsRequired();

        builder.OwnsOne(donante => donante.ContactoPrincipal, contacto =>
        {
            contacto.Property(valor => valor.NombreRepresentante)
                .HasColumnName("contacto_nombre_representante")
                .HasMaxLength(150)
                .IsRequired();

            contacto.Property(valor => valor.Cargo)
                .HasColumnName("contacto_cargo")
                .HasMaxLength(120)
                .IsRequired();

            contacto.Property(valor => valor.EmailContacto)
                .HasColumnName("contacto_email")
                .HasMaxLength(200)
                .IsRequired();

            contacto.Property(valor => valor.TelefonoContacto)
                .HasColumnName("contacto_telefono")
                .HasMaxLength(40)
                .IsRequired();
        });

        builder.Property(donante => donante.EstadoDonante)
            .HasColumnName("estado_donante")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(donante => donante.Moneda)
            .HasColumnName("moneda")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(donante => donante.SaldoTotalHistorico)
            .ComoDinero("saldo_total_historico");

        builder.Property(donante => donante.SaldoDisponibleAsignacion)
            .ComoDinero("saldo_disponible_asignacion");

        builder.HasMany(donante => donante.Aportes)
            .WithOne()
            .HasForeignKey(aporte => aporte.DonanteId)
            .HasConstraintName("fk_aportes_donante_donante")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(donante => donante.Aportes)
            .HasField("_aportes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(donante => donante.IdentificacionFiscal)
            .IsUnique()
            .HasDatabaseName("ux_donante_identificacion_fiscal");
    }
}

