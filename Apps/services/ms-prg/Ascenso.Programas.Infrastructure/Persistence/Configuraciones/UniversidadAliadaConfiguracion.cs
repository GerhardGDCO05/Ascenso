using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class UniversidadAliadaConfiguracion : IEntityTypeConfiguration<UniversidadAliada>
{
    public void Configure(EntityTypeBuilder<UniversidadAliada> builder)
    {
        builder.ToTable("universidades_aliadas", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_universidad_aliada_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoUniversidad>()})");
        });

        builder.HasKey(universidad => universidad.Id).HasName("pk_universidades_aliadas");

        builder.Ignore(universidad => universidad.EventosDominio);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(universidad => universidad.CodigoInstitucional)
            .HasColumnName("codigo_institucional")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(universidad => universidad.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(universidad => universidad.Direccion)
            .HasColumnName("direccion")
            .HasMaxLength(400)
            .IsRequired();

        builder.OwnsOne(universidad => universidad.ContactoEnlace, contacto =>
        {
            contacto.Property(valor => valor.Nombre)
                .HasColumnName("contacto_enlace_nombre")
                .HasMaxLength(150)
                .IsRequired();

            contacto.Property(valor => valor.Email)
                .HasColumnName("contacto_enlace_email")
                .HasMaxLength(200)
                .IsRequired();

            contacto.Property(valor => valor.Telefono)
                .HasColumnName("contacto_enlace_telefono")
                .HasMaxLength(40)
                .IsRequired();
        });

        builder.OwnsOne(universidad => universidad.CuentaBancariaInstitucional, cuenta =>
        {
            cuenta.Property(valor => valor.Banco)
                .HasColumnName("cuenta_banco")
                .HasMaxLength(120);

            cuenta.Property(valor => valor.TipoCuenta)
                .HasColumnName("cuenta_tipo")
                .HasMaxLength(40);

            cuenta.Property(valor => valor.NumeroCuentaCifrado)
                .HasColumnName("cuenta_numero_cifrado")
                .HasMaxLength(200);

            cuenta.Property(valor => valor.RifTitular)
                .HasColumnName("cuenta_rif_titular")
                .HasMaxLength(40);
        });

        builder.Property(universidad => universidad.UsuarioEnlaceId)
            .HasColumnName("usuario_enlace_id")
            .HasColumnType("uuid");

        builder.Property(universidad => universidad.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasMany(universidad => universidad.Convenios)
            .WithOne()
            .HasForeignKey(convenio => convenio.UniversidadAliadaId)
            .HasConstraintName("fk_convenios_universidad_universidad")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(universidad => universidad.Convenios)
            .HasField("_convenios")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(universidad => universidad.Carreras)
            .WithOne()
            .HasForeignKey(carrera => carrera.UniversidadAliadaId)
            .HasConstraintName("fk_carreras_elegibles_universidad")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(universidad => universidad.Carreras)
            .HasField("_carreras")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(universidad => universidad.CodigoInstitucional)
            .IsUnique()
            .HasDatabaseName("ux_universidad_aliada_codigo_institucional");
    }
}
