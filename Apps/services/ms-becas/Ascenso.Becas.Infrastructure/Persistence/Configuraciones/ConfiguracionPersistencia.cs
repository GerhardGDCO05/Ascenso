using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Convenciones compartidas por las configuraciones de persistencia.
/// </summary>
internal static class ConfiguracionPersistencia
{
    public static void ConfigurarAuditoria<TEntidad>(EntityTypeBuilder<TEntidad> builder)
        where TEntidad : EntidadAuditable<Guid>
    {
        builder.Property(entidad => entidad.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

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
            .IsConcurrencyToken();
    }

    public static PropertyBuilder<TEnum> ComoCodigoEnum<TEnum>(this PropertyBuilder<TEnum> builder)
        where TEnum : struct, Enum
        => builder
            .HasConversion(new ValueConverter<TEnum, string>(
                valor => valor.ACodigo(),
                codigo => DesdeCodigo<TEnum>(codigo)))
            .HasColumnType("varchar");

    public static PropertyBuilder<TEnum?> ComoCodigoEnumOpcional<TEnum>(this PropertyBuilder<TEnum?> builder)
        where TEnum : struct, Enum
        => builder
            .HasConversion(new ValueConverter<TEnum?, string?>(
                valor => valor.HasValue ? valor.Value.ACodigo() : null,
                codigo => string.IsNullOrEmpty(codigo)
                    ? null
                    : DesdeCodigo<TEnum>(codigo)))
            .HasColumnType("varchar");

    public static TEnum DesdeCodigo<TEnum>(string codigo)
        where TEnum : struct, Enum
    {
        foreach (var valor in Enum.GetValues<TEnum>())
        {
            if (string.Equals(valor.ACodigo(), codigo, StringComparison.Ordinal))
            {
                return valor;
            }
        }

        throw new ArgumentException(
            $"El código '{codigo}' no corresponde a un valor de {typeof(TEnum).Name}.",
            nameof(codigo));
    }

    public static string ValoresSql<TEnum>()
        where TEnum : struct, Enum
        => string.Join(", ", Enum.GetValues<TEnum>().Select(valor => $"'{valor.ACodigo()}'"));

    public static void ConfigurarMonto(this PropertyBuilder<decimal> builder, string columna)
        => builder
            .HasColumnName(columna)
            .HasPrecision(18, 2);

    public static void ConfigurarPorcentaje(this PropertyBuilder<decimal> builder, string columna)
        => builder
            .HasColumnName(columna)
            .HasPrecision(5, 2);
}
