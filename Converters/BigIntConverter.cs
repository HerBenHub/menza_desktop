using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace menza_admin.Converters
{
    public class BigIntConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Handle bigint as string (e.g., "123456789")
                if (long.TryParse(reader.GetString(), out long result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // Handle regular number
                return reader.GetInt64();
            }

            throw new JsonException($"Unable to convert token type {reader.TokenType} to Int64");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (DateTime.TryParse(dateString, out DateTime result))
                {
                    return result;
                }
            }

            throw new JsonException($"Unable to convert token type {reader.TokenType} to DateTime");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O")); // ISO 8601 format
        }
    }
}