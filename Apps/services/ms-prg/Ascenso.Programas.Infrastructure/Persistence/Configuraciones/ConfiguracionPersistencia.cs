using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Convenciones compartidas por las configuraciones de persistencia de MS-PRG.
/// </summary>
internal static class ConfiguracionPersistencia
{
    public static void ConfigurarEntidadAuditable<TEntidad>(EntityTypeBuilder<TEntidad> builder)
        where TEntidad : EntidadAuditable<Guid>
    {
        builder.Property(entidad => entidad.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(entidad => entidad.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasColumnType("timestamptz");

        builder.Property(entidad => entidad.UsuarioCreacionId)
            .HasColumnName("usuario_creacion_id")
            .HasColumnType("uuid");

        builder.Property(entidad => entidad.FechaUltimaModificacion)
            .HasColumnName("fecha_ultima_modificacion")
            .HasColumnType("timestamptz");

        builder.Property(entidad => entidad.UsuarioModificacionId)
            .HasColumnName("usuario_modificacion_id")
            .HasColumnType("uuid");

        builder.Property(entidad => entidad.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .ValueGeneratedNever();
    }

    public static PropertyBuilder<TEnum> ComoCodigoEnum<TEnum>(this PropertyBuilder<TEnum> builder)
        where TEnum : struct, Enum
        => builder
            .HasConversion(new ValueConverter<TEnum, string>(
                valor => valor.ACodigo(),
                codigo => CodigoEnum.DesdeCodigo<TEnum>(codigo)))
            .HasColumnType("varchar");

    public static PropertyBuilder<TEnum?> ComoCodigoEnumOpcional<TEnum>(this PropertyBuilder<TEnum?> builder)
        where TEnum : struct, Enum
        => builder
            .HasConversion(new ValueConverter<TEnum?, string?>(
                valor => valor.HasValue ? valor.Value.ACodigo() : null,
                codigo => string.IsNullOrEmpty(codigo)
                    ? null
                    : CodigoEnum.DesdeCodigo<TEnum>(codigo)))
            .HasColumnType("varchar");

    public static string ValoresSql<TEnum>()
        where TEnum : struct, Enum
        => string.Join(", ", Enum.GetValues<TEnum>().Select(valor => $"'{valor.ACodigo()}'"));

    public static PropertyBuilder<decimal> ComoDinero(this PropertyBuilder<decimal> builder, string columna)
        => builder
            .HasColumnName(columna)
            .HasPrecision(18, 2);
}

