namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Text.Json;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>Internal endpoint credential resolution and async-flow provider settings.</summary>
    public static class PartioEndpointRuntime
    {
        private sealed record Entry(ProviderSettings Settings, DateTime ExpiresUtc);
        private static readonly ConcurrentDictionary<string, Entry> Known = new();
        private static readonly AsyncLocal<Entry> CurrentSettings = new();
        public static ProviderSettings Current => CurrentSettings.Value?.Settings;
        public static bool IsManaged => CurrentSettings.Value != null;
        public static void Remember(PartioEndpointConfig endpoint)
        {
            if (String.IsNullOrEmpty(endpoint?.Id)) return;
            Known[endpoint.Id] = new Entry(endpoint.ProviderSettings, DateTime.UtcNow.AddHours(2));
            foreach (var entry in Known)
                if (entry.Value.ExpiresUtc < DateTime.UtcNow) Known.TryRemove(entry.Key, out _);
        }
        public static IDisposable Use(string endpointId)
        {
            Entry previous = CurrentSettings.Value;
            CurrentSettings.Value = endpointId != null && Known.TryGetValue(endpointId, out Entry entry) && entry.ExpiresUtc >= DateTime.UtcNow ? entry : null;
            return new Scope(previous);
        }
        private sealed class Scope(Entry previous) : IDisposable
        {
            public void Dispose() => CurrentSettings.Value = previous;
        }
        /// <summary>Reveal only for internal inference; never use this method for a public endpoint read.</summary>
        public static async Task<HttpResponseMessage> ReadAsync(IInferenceEndpointService service, string endpointId, CancellationToken token = default)
        {
            string path = "/v1.0/endpoints/completion/" + Uri.EscapeDataString(endpointId);
            HttpResponseMessage response = await service.SendAsync(HttpMethod.Get, path + "/reveal", null, token).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                await RejectMaskedCredentialsAsync(response, token).ConfigureAwait(false);
                return response;
            }
            response.Dispose();
            // Older Partio versions return plaintext on ordinary reads and have no reveal route.
            response = await service.SendAsync(HttpMethod.Get, path, null, token).ConfigureAwait(false);
            await RejectMaskedCredentialsAsync(response, token).ConfigureAwait(false);
            return response;
        }
        private static async Task RejectMaskedCredentialsAsync(HttpResponseMessage response, CancellationToken token)
        {
            if (!response.IsSuccessStatusCode) return;
            string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            using JsonDocument json = JsonDocument.Parse(body);
            bool Masked(JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Object) return false;
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if ((property.Name.Equals("ApiKey", StringComparison.OrdinalIgnoreCase) || property.Name.Equals("SecretAccessKey", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Equals("SessionToken", StringComparison.OrdinalIgnoreCase) || property.Name.Equals("ServiceAccountJson", StringComparison.OrdinalIgnoreCase))
                        && property.Value.ValueKind == JsonValueKind.String && property.Value.GetString().StartsWith("****", StringComparison.Ordinal)) return true;
                    if (Masked(property.Value)) return true;
                }
                return false;
            }
            if (Masked(json.RootElement))
            {
                response.Dispose();
                throw new InvalidOperationException("Partio returned masked endpoint credentials for an internal credential read. Update the configured Partio service and its administrator access.");
            }
        }
    }
}
