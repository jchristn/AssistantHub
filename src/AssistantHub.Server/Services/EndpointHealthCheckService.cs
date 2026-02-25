namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Background service that performs periodic health checks on both embedding and completion endpoints.
    /// Health state is tracked entirely in RAM and not persisted.
    /// Endpoint config is fetched from Partio via HTTP; this service parses JSON responses directly.
    /// </summary>
    public class EndpointHealthCheckService
    {
        private readonly LoggingModule _Logging;
        private readonly AssistantHubSettings _Settings;
        private readonly string _Header = "[HealthCheck] ";
        private readonly ConcurrentDictionary<string, EndpointHealthState> _States = new ConcurrentDictionary<string, EndpointHealthState>();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _CancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        private readonly ConcurrentDictionary<string, Task> _RunningTasks = new ConcurrentDictionary<string, Task>();
        private readonly HttpClient _HttpClient = new HttpClient();
        private static readonly TimeSpan HistoryRetention = TimeSpan.FromHours(24);

        /// <summary>
        /// Initialize a new EndpointHealthCheckService.
        /// </summary>
        public EndpointHealthCheckService(AssistantHubSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        /// <summary>
        /// Start health checks for all enabled and active endpoints by enumerating from Partio.
        /// </summary>
        public async Task StartAsync()
        {
            _Logging.Info(_Header + "starting health check service");

            int started = 0;

            // Enumerate embedding endpoints from Partio
            started += await EnumerateAndStartAsync("embedding").ConfigureAwait(false);

            // Enumerate completion endpoints from Partio
            started += await EnumerateAndStartAsync("completion").ConfigureAwait(false);

            _Logging.Info(_Header + "health check service started, monitoring " + started + " endpoints");
        }

        /// <summary>
        /// Enumerate endpoints of a given type from Partio and start health checks.
        /// </summary>
        private async Task<int> EnumerateAndStartAsync(string endpointType)
        {
            int started = 0;

            try
            {
                string partioUrl = _Settings.Chunking.Endpoint.TrimEnd('/') + "/v1.0/endpoints/" + endpointType + "/enumerate";

                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, partioUrl);
                req.Headers.Add("Authorization", "Bearer " + _Settings.Chunking.AccessKey);
                req.Content = new StringContent("{\"MaxResults\":1000}", Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await _HttpClient.SendAsync(req).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "failed to enumerate " + endpointType + " endpoints from Partio: HTTP " + (int)resp.StatusCode);
                    return 0;
                }

                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(body);

                if (!doc.RootElement.TryGetProperty("Data", out JsonElement dataArray) || dataArray.ValueKind != JsonValueKind.Array)
                    return 0;

                foreach (JsonElement ep in dataArray.EnumerateArray())
                {
                    EndpointConfig config = ParseEndpointConfig(ep);
                    if (config != null && config.HealthCheckEnabled && config.Active)
                    {
                        StartLoop(config);
                        started++;
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "error enumerating " + endpointType + " endpoints: " + ex.Message);
            }

            return started;
        }

        /// <summary>
        /// Called when a new endpoint is created. Parse the Partio response JSON and start monitoring if applicable.
        /// </summary>
        public void OnEndpointCreated(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson)) return;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseJson);
                EndpointConfig config = ParseEndpointConfig(doc.RootElement);
                if (config != null && config.HealthCheckEnabled && config.Active)
                {
                    StartLoop(config);
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "error parsing created endpoint: " + ex.Message);
            }
        }

        /// <summary>
        /// Called when an endpoint is updated. Parse the Partio response JSON, stop existing loop, and restart if applicable.
        /// </summary>
        public void OnEndpointUpdated(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson)) return;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseJson);
                EndpointConfig config = ParseEndpointConfig(doc.RootElement);
                if (config == null) return;

                StopLoop(config.Id);

                if (config.HealthCheckEnabled && config.Active)
                {
                    StartLoop(config);
                }
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "error parsing updated endpoint: " + ex.Message);
            }
        }

        /// <summary>
        /// Called when an endpoint is deleted. Stop the loop and remove state.
        /// </summary>
        public void OnEndpointDeleted(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            StopLoop(id);
            _States.TryRemove(id, out _);
        }

        /// <summary>
        /// Get the health state for a specific endpoint.
        /// Returns null if no state exists.
        /// </summary>
        public EndpointHealthState? GetHealthState(string endpointId)
        {
            if (_States.TryGetValue(endpointId, out EndpointHealthState? state))
                return state;
            return null;
        }

        /// <summary>
        /// Get health states for all monitored endpoints, optionally filtered by tenant.
        /// </summary>
        public List<EndpointHealthState> GetAllHealthStates(string? tenantId = null)
        {
            List<EndpointHealthState> results = new List<EndpointHealthState>();
            foreach (EndpointHealthState state in _States.Values)
            {
                if (string.IsNullOrEmpty(tenantId) || state.TenantId == tenantId)
                    results.Add(state);
            }
            return results;
        }

        /// <summary>
        /// Returns true if the endpoint is healthy or if no health state exists (health check not enabled).
        /// </summary>
        public bool IsHealthy(string endpointId)
        {
            if (_States.TryGetValue(endpointId, out EndpointHealthState? state))
            {
                lock (state.Lock)
                {
                    return state.IsHealthy;
                }
            }
            return true;
        }

        private void StartLoop(EndpointConfig config)
        {
            EndpointHealthState state = new EndpointHealthState();
            state.EndpointId = config.Id;
            state.EndpointName = config.Model;
            state.TenantId = config.TenantId;
            state.IsHealthy = false;
            state.FirstCheckUtc = DateTime.UtcNow;
            state.LastStateChangeUtc = DateTime.UtcNow;

            _States[config.Id] = state;

            CancellationTokenSource cts = new CancellationTokenSource();
            _CancellationTokens[config.Id] = cts;

            Task loopTask = Task.Run(() => HealthCheckLoopAsync(config, state, cts.Token));
            _RunningTasks[config.Id] = loopTask;

            _Logging.Info(_Header + "started monitoring endpoint " + config.Id + " (" + config.Model + ") every " + config.HealthCheckIntervalMs + "ms");
        }

        private void StopLoop(string endpointId)
        {
            if (_CancellationTokens.TryRemove(endpointId, out CancellationTokenSource? cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (_RunningTasks.TryRemove(endpointId, out Task? task))
            {
                _ = task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _Logging.Warn(_Header + "loop for " + endpointId + " faulted: " + t.Exception?.Message);
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private async Task HealthCheckLoopAsync(EndpointConfig config, EndpointHealthState state, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(config.HealthCheckIntervalMs, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                bool success = false;
                string? errorMessage = null;

                try
                {
                    success = await PerformCheckAsync(config, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    success = false;
                    errorMessage = "health check timed out after " + config.HealthCheckTimeoutMs + "ms";
                    _Logging.Debug(_Header + "timeout for endpoint " + config.Id + " (" + config.Model + "): " + errorMessage);
                }
                catch (Exception ex)
                {
                    success = false;
                    errorMessage = ex.Message;
                    _Logging.Debug(_Header + "error for endpoint " + config.Id + " (" + config.Model + "): " + errorMessage);
                }

                UpdateState(state, success, errorMessage, config);
            }
        }

        private async Task<bool> PerformCheckAsync(EndpointConfig config, CancellationToken token)
        {
            string url = !string.IsNullOrEmpty(config.HealthCheckUrl)
                ? config.HealthCheckUrl
                : config.Endpoint;

            HttpMethod method = string.Equals(config.HealthCheckMethod, "HEAD", StringComparison.OrdinalIgnoreCase)
                ? HttpMethod.Head
                : HttpMethod.Get;

            _Logging.Debug(_Header + "sending " + method + " " + url + " for endpoint " + config.Id + " (" + config.Model + ")");

            HttpRequestMessage request = new HttpRequestMessage(method, url);

            if (config.HealthCheckUseAuth && !string.IsNullOrEmpty(config.ApiKey))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
            }

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(config.HealthCheckTimeoutMs);

            HttpResponseMessage response = await _HttpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
            int statusCode = (int)response.StatusCode;
            bool success = statusCode == config.HealthCheckExpectedStatusCode;

            _Logging.Debug(_Header + "received " + statusCode + " from " + url + " for endpoint " + config.Id + " (" + config.Model + "), success: " + success);

            return success;
        }

        private void UpdateState(EndpointHealthState state, bool success, string? errorMessage, EndpointConfig config)
        {
            DateTime now = DateTime.UtcNow;

            HealthCheckRecord record = new HealthCheckRecord();
            record.TimestampUtc = now;
            record.Success = success;

            lock (state.HistoryLock)
            {
                state.CheckHistory.Add(record);

                DateTime cutoff = now - HistoryRetention;
                state.CheckHistory.RemoveAll(r => r.TimestampUtc < cutoff);
            }

            lock (state.Lock)
            {
                state.LastCheckUtc = now;

                if (success)
                {
                    state.ConsecutiveSuccesses++;
                    state.ConsecutiveFailures = 0;
                    state.LastError = null;

                    if (!state.IsHealthy && state.ConsecutiveSuccesses >= config.HealthyThreshold)
                    {
                        if (state.LastStateChangeUtc.HasValue)
                        {
                            long downtimeMs = (long)(now - state.LastStateChangeUtc.Value).TotalMilliseconds;
                            if (downtimeMs > 0) state.TotalDowntimeMs += downtimeMs;
                        }

                        state.IsHealthy = true;
                        state.LastHealthyUtc = now;
                        state.LastStateChangeUtc = now;

                        _Logging.Info(_Header + "endpoint " + state.EndpointId + " (" + state.EndpointName + ") transitioned to HEALTHY");
                    }
                }
                else
                {
                    state.ConsecutiveFailures++;
                    state.ConsecutiveSuccesses = 0;
                    state.LastError = errorMessage;

                    if (state.IsHealthy && state.ConsecutiveFailures >= config.UnhealthyThreshold)
                    {
                        if (state.LastStateChangeUtc.HasValue)
                        {
                            long uptimeMs = (long)(now - state.LastStateChangeUtc.Value).TotalMilliseconds;
                            if (uptimeMs > 0) state.TotalUptimeMs += uptimeMs;
                        }

                        state.IsHealthy = false;
                        state.LastUnhealthyUtc = now;
                        state.LastStateChangeUtc = now;

                        _Logging.Warn(_Header + "endpoint " + state.EndpointId + " (" + state.EndpointName + ") transitioned to UNHEALTHY: " + (errorMessage ?? "check failed"));
                    }
                }
            }
        }

        /// <summary>
        /// Parse endpoint health config fields from a JSON element (Partio response).
        /// </summary>
        private EndpointConfig? ParseEndpointConfig(JsonElement element)
        {
            try
            {
                EndpointConfig config = new EndpointConfig();

                config.Id = element.TryGetProperty("Id", out JsonElement id) && id.ValueKind == JsonValueKind.String ? id.GetString()! : string.Empty;
                config.Model = element.TryGetProperty("Model", out JsonElement model) && model.ValueKind == JsonValueKind.String ? model.GetString()! : string.Empty;
                config.TenantId = element.TryGetProperty("TenantId", out JsonElement tid) && tid.ValueKind == JsonValueKind.String ? tid.GetString()! : "default";
                config.Endpoint = element.TryGetProperty("Endpoint", out JsonElement ep) && ep.ValueKind == JsonValueKind.String ? ep.GetString()! : string.Empty;
                config.ApiKey = element.TryGetProperty("ApiKey", out JsonElement ak) && ak.ValueKind == JsonValueKind.String ? ak.GetString() : null;
                config.Active = element.TryGetProperty("Active", out JsonElement active) && active.ValueKind == JsonValueKind.True;
                config.HealthCheckEnabled = element.TryGetProperty("HealthCheckEnabled", out JsonElement hce) && hce.ValueKind == JsonValueKind.True;
                config.HealthCheckUrl = element.TryGetProperty("HealthCheckUrl", out JsonElement hcu) && hcu.ValueKind == JsonValueKind.String ? hcu.GetString() : null;

                // HealthCheckMethod can be a string ("GET"/"HEAD") or an int (0/1)
                if (element.TryGetProperty("HealthCheckMethod", out JsonElement hcm))
                {
                    if (hcm.ValueKind == JsonValueKind.String)
                        config.HealthCheckMethod = hcm.GetString()!;
                    else if (hcm.ValueKind == JsonValueKind.Number)
                        config.HealthCheckMethod = hcm.GetInt32() == 1 ? "HEAD" : "GET";
                    else
                        config.HealthCheckMethod = "GET";
                }

                config.HealthCheckIntervalMs = element.TryGetProperty("HealthCheckIntervalMs", out JsonElement hci) && hci.ValueKind == JsonValueKind.Number ? hci.GetInt32() : 30000;
                config.HealthCheckTimeoutMs = element.TryGetProperty("HealthCheckTimeoutMs", out JsonElement hct) && hct.ValueKind == JsonValueKind.Number ? hct.GetInt32() : 10000;
                config.HealthCheckExpectedStatusCode = element.TryGetProperty("HealthCheckExpectedStatusCode", out JsonElement hcesc) && hcesc.ValueKind == JsonValueKind.Number ? hcesc.GetInt32() : 200;
                config.HealthyThreshold = element.TryGetProperty("HealthyThreshold", out JsonElement ht) && ht.ValueKind == JsonValueKind.Number ? ht.GetInt32() : 2;
                config.UnhealthyThreshold = element.TryGetProperty("UnhealthyThreshold", out JsonElement ut) && ut.ValueKind == JsonValueKind.Number ? ut.GetInt32() : 2;
                config.HealthCheckUseAuth = element.TryGetProperty("HealthCheckUseAuth", out JsonElement hcua) && hcua.ValueKind == JsonValueKind.True;

                if (string.IsNullOrEmpty(config.Id)) return null;

                return config;
            }
            catch (Exception ex)
            {
                _Logging.Warn(_Header + "failed to parse endpoint config: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Internal config holder for parsed endpoint JSON.
        /// </summary>
        private class EndpointConfig
        {
            public string Id { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public string TenantId { get; set; } = "default";
            public string Endpoint { get; set; } = string.Empty;
            public string? ApiKey { get; set; }
            public bool Active { get; set; }
            public bool HealthCheckEnabled { get; set; }
            public string? HealthCheckUrl { get; set; }
            public string HealthCheckMethod { get; set; } = "GET";
            public int HealthCheckIntervalMs { get; set; } = 30000;
            public int HealthCheckTimeoutMs { get; set; } = 10000;
            public int HealthCheckExpectedStatusCode { get; set; } = 200;
            public int HealthyThreshold { get; set; } = 2;
            public int UnhealthyThreshold { get; set; } = 2;
            public bool HealthCheckUseAuth { get; set; }
        }
    }
}
