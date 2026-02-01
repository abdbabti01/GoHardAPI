using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoHardAPI.Converters;

/// <summary>
/// Serializes DateTime as date-only string "yyyy-MM-dd" for fields that don't need time.
/// This prevents timezone-related date shifts when dates are transferred between server and client.
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateStr = reader.GetString();
        if (string.IsNullOrEmpty(dateStr))
        {
            return default;
        }
        return DateTime.Parse(dateStr);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}

/// <summary>
/// Nullable version of DateOnlyJsonConverter for optional date fields.
/// </summary>
public class NullableDateOnlyJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        var dateStr = reader.GetString();
        if (string.IsNullOrEmpty(dateStr))
        {
            return null;
        }
        return DateTime.Parse(dateStr);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
