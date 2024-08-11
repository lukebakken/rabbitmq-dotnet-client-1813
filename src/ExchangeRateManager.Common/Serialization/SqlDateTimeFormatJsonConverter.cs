using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExchangeRateManager.Common.Serialization;

/// <summary>
/// Converts SQL Formatted dates into datetime objects and vice versa
/// </summary>

public class SqlDateTimeFormatJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTimeString = reader.GetString();
        return DateTime.ParseExact(dateTimeString!, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}