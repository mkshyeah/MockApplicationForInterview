using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountingHelper.API.Serialization;

/// <summary>
/// Writes enums as strings and rejects anything that is not a declared member.
/// Replaces the default converter so that the client sees the allowed values
/// instead of a message containing the internal CLR type name.
/// Numeric input is rejected as well: the API accepts enum names only.
/// </summary>
public sealed class StrictEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(StrictEnumConverter<>).MakeGenericType(typeToConvert))!;

    private sealed class StrictEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        // Enum.TryParse is deliberately not used: it also accepts numeric strings ("3")
        // and comma-separated lists that are combined bitwise even for non-[Flags] enums
        // ("Annual, Sick" => 1 | 2 => 3 => Unpaid), both of which pass Enum.IsDefined.
        // Matching declared names directly is the only way to accept names and nothing else.
        // Built from names, not values: two members sharing one value would both stringify to the
        // same name and blow up the static initializer with a duplicate key.
        private static readonly Dictionary<string, TEnum> ByName =
            Enum.GetNames<TEnum>().ToDictionary(
                name => name,
                Enum.Parse<TEnum>,
                StringComparer.OrdinalIgnoreCase);

        private static readonly string AllowedValues = string.Join(", ", Enum.GetNames<TEnum>());

        public override TEnum Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var raw = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

            if (raw is not null && ByName.TryGetValue(raw, out var parsed))
            {
                return parsed;
            }

            // The error key is already the offending field name (e.g. leaveType), so the
            // message reads as its continuation. System.Text.Json appends the path itself.
            throw new JsonException($"must be one of: {AllowedValues}");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
