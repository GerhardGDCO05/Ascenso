using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Api.Controllers.v1;

public sealed class CodigoEnumJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var codigo = reader.GetString();
            foreach (var valor in Enum.GetValues<TEnum>())
            {
                if (string.Equals(valor.ACodigo(), codigo, StringComparison.Ordinal))
                    return valor;
            }
        }

        throw new JsonException($"Código inválido para {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value,
        JsonSerializerOptions options)
        => writer.WriteStringValue(value.ACodigo());
}
