namespace AssistantHub.Core.Helpers
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// JSON serialization helper.
    /// </summary>
    public static class Serializer
    {
        #region Private-Members

        private static JsonSerializerOptions _Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Serialize an object to JSON.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty print.</param>
        /// <returns>JSON string.</returns>
        public static string SerializeJson(object obj, bool pretty = true)
        {
            if (obj == null) return null;
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = pretty,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// Deserialize JSON to an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <returns>Object.</returns>
        public static T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) return default;
            return JsonSerializer.Deserialize<T>(json, _Options);
        }

        /// <summary>
        /// Copy an object by serialization and deserialization.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="obj">Object.</param>
        /// <returns>Copy.</returns>
        public static T CopyObject<T>(T obj)
        {
            if (obj == null) return default;
            string json = SerializeJson(obj);
            return DeserializeJson<T>(json);
        }

        #endregion
    }
}
