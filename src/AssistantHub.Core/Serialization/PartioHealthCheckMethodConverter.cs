namespace AssistantHub.Core.Serialization
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Converts Partio health check method values from either string or numeric enum form.
    /// </summary>
    public class PartioHealthCheckMethodConverter : JsonConverter<string>
    {
        /// <inheritdoc />
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? "GET";
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                int value = reader.GetInt32();
                return value == 1 ? "HEAD" : "GET";
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                return "GET";
            }

            throw new JsonException("Unsupported health check method token.");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value ?? "GET");
        }
    }
}
