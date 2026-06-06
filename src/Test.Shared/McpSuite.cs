namespace Test.Automated
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Net.WebSockets;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Sdk.Models;
    using Test.Shared;

    /// <summary>
    /// End-to-end MCP integration tests against the real AssistantHub and MCP server processes.
    /// </summary>
    public class McpSuite : SuiteBase
    {
        /// <summary>
        /// Run the MCP suite.
        /// </summary>
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            await using AssistantHubMcpHost host = await AssistantHubMcpHost.CreateAsync().ConfigureAwait(false);

            string? createdTenantId = null;
            string? createdAssistantId = null;
            string? capturedRequestId = null;
            DateTime requestHistoryStartUtc = DateTime.UtcNow.AddSeconds(-5);
            string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            await ExecuteTestAsync("MCP.Transport.Tcp.AcceptsConnections", async () =>
            {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", host.McpTcpPort).ConfigureAwait(false);
                AssertHelper.IsTrue(client.Connected, "TCP transport should accept a socket connection");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Transport.WebSocket.AcceptsConnections", async () =>
            {
                using ClientWebSocket client = new ClientWebSocket();
                await client.ConnectAsync(new Uri(host.McpWebSocketEndpoint), CancellationToken.None).ConfigureAwait(false);
                AssertHelper.AreEqual(WebSocketState.Open, client.State, "WebSocket transport state");
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.System.Health", async () =>
            {
                string resultJson = await host.Client.CallAsync<string>("system/health", new { }).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(resultJson);
                bool healthy = doc.RootElement.GetProperty("Healthy").GetBoolean();
                AssertHelper.IsTrue(healthy, "system/health should report a healthy upstream");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.System.OpenApi", async () =>
            {
                string resultJson = await host.Client.CallAsync<string>("system/openapi", new { versioned = true }).ConfigureAwait(false);
                AssertHelper.StringContains(resultJson, "openapi", "system/openapi payload");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Tenant.Create", async () =>
            {
                TenantMetadata tenant = new TenantMetadata
                {
                    Name = "mcp-tenant-" + uniqueSuffix,
                    Active = true
                };

                string resultJson = await host.Client.CallAsync<string>(
                    "tenant/create",
                    new { tenantJson = Serializer.SerializeJson(tenant, false) }).ConfigureAwait(false);

                using JsonDocument doc = JsonDocument.Parse(resultJson);
                JsonElement createdTenant = doc.RootElement.GetProperty("Tenant");
                createdTenantId = createdTenant.GetProperty("Id").GetString();

                AssertHelper.IsNotNull(createdTenantId, "created tenant ID");
                AssertHelper.StartsWith(createdTenantId!, "ten_", "created tenant ID prefix");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Tenant.Get", async () =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                string resultJson = await host.Client.CallAsync<string>(
                    "tenant/get",
                    new { tenantId = createdTenantId }).ConfigureAwait(false);

                TenantMetadata? tenant = Serializer.DeserializeJson<TenantMetadata>(resultJson);
                AssertHelper.IsNotNull(tenant, "tenant/get result");
                AssertHelper.AreEqual(createdTenantId, tenant!.Id, "tenant/get identifier");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Tenant.Exists", async () =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                bool exists = await host.Client.CallAsync<bool>(
                    "tenant/exists",
                    new { tenantId = createdTenantId }).ConfigureAwait(false);
                AssertHelper.IsTrue(exists, "tenant/exists should return true for the created tenant");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Assistant.Create", async () =>
            {
                Assistant assistant = new Assistant
                {
                    Name = "mcp-assistant-" + uniqueSuffix,
                    Description = "Assistant created through the MCP integration suite"
                };

                string resultJson = await host.Client.CallAsync<string>(
                    "assistant/create",
                    new { assistantJson = Serializer.SerializeJson(assistant, false) }).ConfigureAwait(false);

                Assistant? createdAssistant = Serializer.DeserializeJson<Assistant>(resultJson);
                AssertHelper.IsNotNull(createdAssistant, "assistant/create result");
                AssertHelper.IsNotNull(createdAssistant!.Id, "created assistant ID");
                AssertHelper.StartsWith(createdAssistant.Id!, "asst_", "created assistant ID prefix");
                createdAssistantId = createdAssistant.Id;
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Assistant.Get", async () =>
            {
                AssertHelper.IsNotNull(createdAssistantId, "createdAssistantId from previous test");
                string resultJson = await host.Client.CallAsync<string>(
                    "assistant/get",
                    new { assistantId = createdAssistantId }).ConfigureAwait(false);

                Assistant? assistant = Serializer.DeserializeJson<Assistant>(resultJson);
                AssertHelper.IsNotNull(assistant, "assistant/get result");
                AssertHelper.AreEqual(createdAssistantId, assistant!.Id, "assistant/get identifier");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Configuration.GetRedacted", async () =>
            {
                string resultJson = await host.Client.CallAsync<string>("configuration/get", new { }).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(resultJson);

                AssertHelper.AreEqual("[REDACTED]", doc.RootElement.GetProperty("S3").GetProperty("AccessKey").GetString(), "S3 access key redaction");
                AssertHelper.AreEqual("[REDACTED]", doc.RootElement.GetProperty("S3").GetProperty("SecretKey").GetString(), "S3 secret key redaction");
                AssertHelper.AreEqual("[REDACTED]", doc.RootElement.GetProperty("Chunking").GetProperty("AccessKey").GetString(), "Chunking access key redaction");
                AssertHelper.AreEqual("[REDACTED]", doc.RootElement.GetProperty("RecallDb").GetProperty("AccessKey").GetString(), "RecallDb access key redaction");
                AssertHelper.AreEqual("[REDACTED]", doc.RootElement.GetProperty("Verbex").GetProperty("AccessKey").GetString(), "Verbex access key redaction");
                AssertHelper.AreEqual("[REDACTED]", doc.RootElement.GetProperty("Inference").GetProperty("ApiKey").GetString(), "Inference API key redaction");
                AssertHelper.AreEqual("[REDACTED]", doc.RootElement.GetProperty("AdminApiKeys")[0].GetString(), "Admin API keys redaction");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Configuration.GetWithSecrets", async () =>
            {
                string resultJson = await host.Client.CallAsync<string>(
                    "configuration/get",
                    new { includeSecrets = true }).ConfigureAwait(false);

                using JsonDocument doc = JsonDocument.Parse(resultJson);
                AssertHelper.AreEqual("default", doc.RootElement.GetProperty("S3").GetProperty("AccessKey").GetString(), "S3 access key when includeSecrets=true");
                AssertHelper.AreEqual("default", doc.RootElement.GetProperty("S3").GetProperty("SecretKey").GetString(), "S3 secret key when includeSecrets=true");
                AssertHelper.AreEqual("default", doc.RootElement.GetProperty("Verbex").GetProperty("AccessKey").GetString(), "Verbex access key when includeSecrets=true");
                AssertHelper.AreEqual("assistanthubadmin", doc.RootElement.GetProperty("AdminApiKeys")[0].GetString(), "Admin API key when includeSecrets=true");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.RequestHistory.CaptureAndList", async () =>
            {
                await host.Client.CallAsync<string>("system/whoami", new { }).ConfigureAwait(false);
                string? lastResultJson = null;

                for (int attempt = 0; attempt < 20; attempt++)
                {
                    RequestHistorySearchFilter filter = new RequestHistorySearchFilter
                    {
                        MaxResults = 50,
                        StartUtc = requestHistoryStartUtc
                    };

                    string resultJson = await host.Client.CallAsync<string>(
                        "requesthistory/list",
                        new { filterJson = Serializer.SerializeJson(filter, false) }).ConfigureAwait(false);
                    lastResultJson = resultJson;

                    EnumerationResult<RequestHistoryEntry>? result = Serializer.DeserializeJson<EnumerationResult<RequestHistoryEntry>>(resultJson);
                    AssertHelper.IsNotNull(result, "requesthistory/list result");
                    AssertHelper.IsNotNull(result!.Objects, "requesthistory/list objects");

                    RequestHistoryEntry? entry = null;
                    foreach (RequestHistoryEntry item in result.Objects)
                    {
                        if (!string.IsNullOrWhiteSpace(item.RequestPath) && item.RequestPath.Contains("/v1.0/whoami", StringComparison.Ordinal))
                        {
                            entry = item;
                            break;
                        }
                    }

                    if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                    {
                        capturedRequestId = entry.Id;
                        return;
                    }

                    await Task.Delay(500).ConfigureAwait(false);
                }

                throw new Exception(
                    "Timed out waiting for request-history capture of /v1.0/whoami."
                    + Environment.NewLine
                    + "Last requesthistory/list payload:"
                    + Environment.NewLine
                    + (lastResultJson ?? "<null>"));
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.RequestHistory.DetailAndSummary", async () =>
            {
                AssertHelper.IsNotNull(capturedRequestId, "capturedRequestId from previous test");

                string detailJson = await host.Client.CallAsync<string>(
                    "requesthistory/detail",
                    new { requestId = capturedRequestId }).ConfigureAwait(false);
                RequestHistoryEntry? detail = Serializer.DeserializeJson<RequestHistoryEntry>(detailJson);
                AssertHelper.IsNotNull(detail, "requesthistory/detail result");
                AssertHelper.AreEqual(capturedRequestId, detail!.Id, "requesthistory/detail identifier");

                RequestHistorySearchFilter filter = new RequestHistorySearchFilter
                {
                    PathContains = "/v1.0/whoami",
                    StartUtc = requestHistoryStartUtc,
                    BucketSeconds = 60
                };

                string summaryJson = await host.Client.CallAsync<string>(
                    "requesthistory/summary",
                    new { filterJson = Serializer.SerializeJson(filter, false) }).ConfigureAwait(false);
                RequestHistorySummaryResult? summary = Serializer.DeserializeJson<RequestHistorySummaryResult>(summaryJson);
                AssertHelper.IsNotNull(summary, "requesthistory/summary result");
                if (summary!.TotalCount < 1)
                {
                    throw new Exception(
                        "Expected requesthistory summary total count to be >= 1, but was "
                        + summary.TotalCount
                        + "."
                        + Environment.NewLine
                        + "MCP summary payload:"
                        + Environment.NewLine
                        + summaryJson);
                }
                AssertHelper.IsGreaterThanOrEqual(summary!.TotalCount, 1, "requesthistory summary total count");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Install.DryRun", async () =>
            {
                string fakeHome = Path.Combine(host.ArtifactDirectory, "dry-run-home");
                Directory.CreateDirectory(fakeHome);

                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet")
                {
                    WorkingDirectory = host.ArtifactDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add(host.McpAssemblyPath);
                startInfo.ArgumentList.Add("install");
                startInfo.ArgumentList.Add("--dry-run");
                startInfo.Environment["USERPROFILE"] = fakeHome;
                startInfo.Environment["HOME"] = fakeHome;

                using Process process = Process.Start(startInfo)
                    ?? throw new InvalidOperationException("Unable to start AssistantHub.McpServer for install dry-run verification.");

                string stdout = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                string stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                await process.WaitForExitAsync().ConfigureAwait(false);

                AssertHelper.AreEqual(0, process.ExitCode, "install --dry-run exit code");
                AssertHelper.StringContains(stdout, "Cursor (.cursor/mcp.json):", "install --dry-run Cursor snippet");
                AssertHelper.StringContains(stdout, "[DRY RUN] No files were modified.", "install --dry-run completion message");
                AssertHelper.IsTrue(string.IsNullOrWhiteSpace(stderr), "install --dry-run should not write stderr");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Assistant.Delete", async () =>
            {
                if (string.IsNullOrWhiteSpace(createdAssistantId))
                    return;

                bool deleted = await host.Client.CallAsync<bool>(
                    "assistant/delete",
                    new { assistantId = createdAssistantId }).ConfigureAwait(false);
                AssertHelper.IsTrue(deleted, "assistant/delete should return true");
                createdAssistantId = null;
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Tenant.Delete", async () =>
            {
                if (string.IsNullOrWhiteSpace(createdTenantId))
                    return;

                bool deleted = await host.Client.CallAsync<bool>(
                    "tenant/delete",
                    new { tenantId = createdTenantId }).ConfigureAwait(false);
                AssertHelper.IsTrue(deleted, "tenant/delete should return true");
                createdTenantId = null;
            }).ConfigureAwait(false);

            return GetResults();
        }
    }
}
