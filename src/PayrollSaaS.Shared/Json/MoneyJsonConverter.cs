using System.Text.Json;
using System.Text.Json.Serialization;
using PayrollSaaS.Shared.Money;

namespace PayrollSaaS.Shared.Json;

/// <summary>
/// Design doc section 6 (API Standards): "Money in JSON -- returned as string to avoid
/// floating-point drift (e.g. \"42895.0000\")". Registered globally so no endpoint can forget.
/// Reads accept either a JSON string or a JSON number so clients are not forced to quote input.
/// </summary>
public sealed class MoneyJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => decimal.Parse(reader.GetString()!, System.Globalization.NumberStyles.Number,
                                                  System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.Number => reader.GetDecimal(),
            _ => throw new JsonException($"Expected a string or number for a monetary value, got {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteStringValue(MoneyMath.ToApiString(value));
}

/// <summary>Nullable counterpart -- System.Text.Json does not unwrap Nullable&lt;T&gt; automatically.</summary>
public sealed class NullableMoneyJsonConverter : JsonConverter<decimal?>
{
    private static readonly MoneyJsonConverter Inner = new();

    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : Inner.Read(ref reader, typeof(decimal), options);

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else Inner.Write(writer, value.Value, options);
    }
}
