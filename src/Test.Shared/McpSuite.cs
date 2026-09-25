namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
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
    using Voltaic.Core;
    using Voltaic.Mcp;

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

            await ExecuteTestAsync("MCP.Protocol.Ping.ReturnsEmptyObject", async () =>
            {
                await host.Client.PingAsync().ConfigureAwait(false);

                JsonRpcResponse response = await host.Client.CallAsync("ping").ConfigureAwait(false);
                AssertHelper.IsNull(response.Error, "ping should not return an error");
                AssertHelper.AreEqual("{}", JsonSerializer.Serialize(response.Result), "ping result should be an empty object");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Protocol.ToolsList.OnlyApplicationTools", async () =>
            {
                List<string> toolNames = await ListAllToolNamesAsync(host).ConfigureAwait(false);

                AssertHelper.IsTrue(toolNames.Contains("system/health"), "tools/list should include system/health");
                AssertHelper.IsTrue(toolNames.Contains("tenant/create"), "tools/list should include tenant/create");
                AssertHelper.IsTrue(toolNames.Contains("configuration/get"), "tools/list should include configuration/get");

                foreach (string removed in new[] { "ping", "echo", "getTime", "getSessions", "getClients" })
                {
                    AssertHelper.IsFalse(toolNames.Contains(removed), "tools/list should not publish the Voltaic demo tool '" + removed + "'");
                }

                AssertHelper.AreEqual(toolNames.Count, new HashSet<string>(toolNames).Count, "tools/list should not contain duplicate tool names");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Protocol.BareToolMethod.Rejected", async () =>
            {
                JsonRpcResponse response = await host.Client.CallAsync("system/health", new { }).ConfigureAwait(false);
                AssertHelper.IsNotNull(response.Error, "calling a tool as a bare JSON-RPC method should fail");
                AssertHelper.AreEqual(-32601, response.Error!.Code, "bare tool method error code");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Protocol.DemoTool.Rejected", async () =>
            {
                JsonRpcResponse response = await host.Client.CallAsync(
                    "tools/call",
                    new { name = "echo", arguments = new { message = "hello" } }).ConfigureAwait(false);
                AssertHelper.IsNotNull(response.Error, "tools/call for the removed echo demo tool should fail");
                AssertHelper.AreEqual(-32602, response.Error!.Code, "unknown tool error code");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Protocol.ToolsCall.MissingRequiredArgument.Rejected", async () =>
            {
                JsonRpcResponse response = await host.Client.CallAsync(
                    "tools/call",
                    new { name = "tenant/get", arguments = new { } }).ConfigureAwait(false);
                AssertHelper.IsNotNull(response.Error, "tools/call without a required argument should fail");
                AssertHelper.AreEqual(-32602, response.Error!.Code, "missing required argument error code");
                AssertHelper.StringContains(response.Error.Message, "tenantId", "missing required argument error message");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Transport.Tcp.MethodsAndPing", async () =>
            {
                using McpTcpClient client = new McpTcpClient();
                bool connected = await client.ConnectAsync("127.0.0.1", host.McpTcpPort).ConfigureAwait(false);
                AssertHelper.IsTrue(connected, "TCP MCP client should connect");

                await client.CallAsync<object?>("ping").ConfigureAwait(false);

                string resultJson = await client.CallAsync<string>("system/health", new { }).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(resultJson);
                AssertHelper.IsTrue(doc.RootElement.GetProperty("Healthy").GetBoolean(), "TCP system/health should report a healthy upstream");

                Exception? echoError = null;
                try
                {
                    await client.CallAsync<object?>("echo", new { message = "hello" }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    echoError = e;
                }

                AssertHelper.IsNotNull(echoError, "TCP transport should no longer expose the Voltaic echo method");
                AssertHelper.StringContains(echoError!.Message, "-32601", "TCP echo error code");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.System.Health", async () =>
            {
                string resultJson = await CallToolTextAsync(host, "system/health", new { }).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(resultJson);
                bool healthy = doc.RootElement.GetProperty("Healthy").GetBoolean();
                AssertHelper.IsTrue(healthy, "system/health should report a healthy upstream");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.System.OpenApi", async () =>
            {
                string resultJson = await CallToolTextAsync(host, "system/openapi", new { versioned = true }).ConfigureAwait(false);
                AssertHelper.StringContains(resultJson, "openapi", "system/openapi payload");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Tenant.Create", async () =>
            {
                TenantMetadata tenant = new TenantMetadata
                {
                    Name = "mcp-tenant-" + uniqueSuffix,
                    Active = true
                };

                string resultJson = await CallToolTextAsync(host, 
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
                string resultJson = await CallToolTextAsync(host, 
                    "tenant/get",
                    new { tenantId = createdTenantId }).ConfigureAwait(false);

                TenantMetadata? tenant = Serializer.DeserializeJson<TenantMetadata>(resultJson);
                AssertHelper.IsNotNull(tenant, "tenant/get result");
                AssertHelper.AreEqual(createdTenantId, tenant!.Id, "tenant/get identifier");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Tenant.Exists", async () =>
            {
                AssertHelper.IsNotNull(createdTenantId, "createdTenantId from previous test");
                bool exists = await CallToolBoolAsync(host, 
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

                string resultJson = await CallToolTextAsync(host, 
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
                string resultJson = await CallToolTextAsync(host, 
                    "assistant/get",
                    new { assistantId = createdAssistantId }).ConfigureAwait(false);

                Assistant? assistant = Serializer.DeserializeJson<Assistant>(resultJson);
                AssertHelper.IsNotNull(assistant, "assistant/get result");
                AssertHelper.AreEqual(createdAssistantId, assistant!.Id, "assistant/get identifier");
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Configuration.GetRedacted", async () =>
            {
                string resultJson = await CallToolTextAsync(host, "configuration/get", new { }).ConfigureAwait(false);
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
                string resultJson = await CallToolTextAsync(host, 
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
                await CallToolTextAsync(host, "system/whoami", new { }).ConfigureAwait(false);
                string? lastResultJson = null;

                for (int attempt = 0; attempt < 20; attempt++)
                {
                    RequestHistorySearchFilter filter = new RequestHistorySearchFilter
                    {
                        MaxResults = 50,
                        StartUtc = requestHistoryStartUtc
                    };

                    string resultJson = await CallToolTextAsync(host, 
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

                string detailJson = await CallToolTextAsync(host, 
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

                string summaryJson = await CallToolTextAsync(host, 
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

                bool deleted = await CallToolBoolAsync(host, 
                    "assistant/delete",
                    new { assistantId = createdAssistantId }).ConfigureAwait(false);
                AssertHelper.IsTrue(deleted, "assistant/delete should return true");
                createdAssistantId = null;
            }).ConfigureAwait(false);

            await ExecuteTestAsync("MCP.Tenant.Delete", async () =>
            {
                if (string.IsNullOrWhiteSpace(createdTenantId))
                    return;

                bool deleted = await CallToolBoolAsync(host, 
                    "tenant/delete",
                    new { tenantId = createdTenantId }).ConfigureAwait(false);
                AssertHelper.IsTrue(deleted, "tenant/delete should return true");
                createdTenantId = null;
            }).ConfigureAwait(false);

            return GetResults();
        }

        private static async Task<string> CallToolTextAsync(AssistantHubMcpHost host, string toolName, object arguments)
        {
            McpToolCallTextResult result = await host.Client.CallAsync<McpToolCallTextResult>(
                "tools/call",
                new { name = toolName, arguments = arguments }).ConfigureAwait(false);

            if (result == null)
                throw new Exception("tools/call for " + toolName + " returned no result.");
            if (result.IsError == true)
                throw new Exception("tools/call for " + toolName + " reported an error: " + (result.Content.Count > 0 ? result.Content[0].Text : "<none>"));
            if (result.Content.Count < 1 || result.Content[0].Text == null)
                throw new Exception("tools/call for " + toolName + " returned no text content.");

            return result.Content[0].Text!;
        }

        private static async Task<bool> CallToolBoolAsync(AssistantHubMcpHost host, string toolName, object arguments)
        {
            string text = await CallToolTextAsync(host, toolName, arguments).ConfigureAwait(false);
            return bool.Parse(text);
        }

        private static async Task<List<string>> ListAllToolNamesAsync(AssistantHubMcpHost host)
        {
            List<string> names = new List<string>();
            string? cursor = null;

            do
            {
                McpToolListView page = cursor == null
                    ? await host.Client.CallAsync<McpToolListView>("tools/list", new { }).ConfigureAwait(false)
                    : await host.Client.CallAsync<McpToolListView>("tools/list", new { cursor = cursor }).ConfigureAwait(false);

                foreach (McpToolNameView tool in page.Tools)
                {
                    if (!string.IsNullOrEmpty(tool.Name))
                        names.Add(tool.Name);
                }

                cursor = page.NextCursor;
            }
            while (!string.IsNullOrEmpty(cursor));

            return names;
        }
    }
}
