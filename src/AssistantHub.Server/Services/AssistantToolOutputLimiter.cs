namespace AssistantHub.Server.Services
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Applies per-call and per-turn model-visible tool output limits.
    /// </summary>
    public static class AssistantToolOutputLimiter
    {
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Serialize and limit one tool result.
        /// </summary>
        /// <param name="result">Tool execution result to populate.</param>
        /// <param name="output">Serializable output.</param>
        /// <param name="maxChars">Maximum output characters.</param>
        public static void ApplyPerCallLimit(AssistantToolExecutionResult result, object output, int maxChars)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            string json = JsonSerializer.Serialize(output, _JsonOptions);
            result.OutputCharacters = json.Length;
            result.OutputJson = LimitJson(json, Math.Max(1, maxChars), out bool truncated);
            result.Truncated = result.Truncated || truncated;
        }

        /// <summary>
        /// Apply the remaining turn-level output budget to already serialized model-visible JSON.
        /// </summary>
        /// <param name="json">Model-visible JSON.</param>
        /// <param name="remainingChars">Remaining turn budget.</param>
        /// <param name="truncated">Whether output was truncated.</param>
        /// <returns>Limited model-visible JSON.</returns>
        public static string ApplyTurnLimit(string json, int remainingChars, out bool truncated)
        {
            string value = String.IsNullOrWhiteSpace(json) ? "{}" : json;
            return LimitJson(value, Math.Max(1, remainingChars), out truncated);
        }

        private static string LimitJson(string json, int maxChars, out bool truncated)
        {
            truncated = false;
            string value = String.IsNullOrWhiteSpace(json) ? "{}" : json;
            if (value.Length <= maxChars)
                return value;

            truncated = true;
            return JsonSerializer.Serialize(new
            {
                Truncated = true,
                OriginalCharacters = value.Length,
                Content = value.Substring(0, maxChars)
            }, _JsonOptions);
        }
    }
}
