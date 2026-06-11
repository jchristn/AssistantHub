namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using Test.Shared;

    public class IntegrationSuite : SuiteBase
    {
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            TestServer server = await TestServer.CreateAsync();

            try
            {
                // ===== ServerLifecycleTests =====

                await ExecuteTestAsync("Server.RootEndpoint_Returns200", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "GET / should return 200");
                });

                await ExecuteTestAsync("Server.RootEndpoint_ReturnsJson", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/");
                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsNotNull(body, "response body should not be null");
                    AssertHelper.IsTrue(body.Length > 0, "response body should not be empty");
                });

                await ExecuteTestAsync("Server.NonExistentEndpoint_Returns404", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent endpoint should return 404");
                });

                await ExecuteTestAsync("Server.UnauthenticatedRequest_Returns401", async () =>
                {
                    using HttpClient noAuthClient = new HttpClient();
                    noAuthClient.BaseAddress = new Uri(server.BaseUrl);

                    HttpResponseMessage resp = await noAuthClient.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.Unauthorized, (int)resp.StatusCode, "unauthenticated request should return 401");
                });

                await ExecuteTestAsync("Server.Swagger_Returns200WithoutAuthentication", async () =>
                {
                    using HttpClient noAuthClient = new HttpClient();
                    noAuthClient.BaseAddress = new Uri(server.BaseUrl);

                    HttpResponseMessage swaggerResp = await noAuthClient.GetAsync("/swagger");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)swaggerResp.StatusCode, "Swagger UI should be unauthenticated");

                    string swaggerBody = await swaggerResp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(swaggerBody, "SwaggerUIBundle", "Swagger UI bundle reference");
                    AssertHelper.StringContains(swaggerBody, "/openapi.json", "Swagger UI OpenAPI document route");

                    HttpResponseMessage openApiResp = await noAuthClient.GetAsync("/openapi.json");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)openApiResp.StatusCode, "OpenAPI should be unauthenticated");

                    string openApiBody = await openApiResp.Content.ReadAsStringAsync();
                    using JsonDocument openApiDocument = JsonDocument.Parse(openApiBody);
                    JsonElement securitySchemes = openApiDocument.RootElement.GetProperty("components").GetProperty("securitySchemes");
                    JsonElement bearerAuth;
                    AssertHelper.StringContains(openApiBody, "\"/swagger\"", "Runtime OpenAPI Swagger route");
                    AssertHelper.StringContains(openApiBody, "\"security\": []", "Runtime OpenAPI unauthenticated route marker");
                    AssertHelper.StringContains(openApiBody, "\"/v1.0/tenants\"", "Runtime OpenAPI protected tenant route");
                    AssertHelper.StringContains(openApiBody, "\"BearerAuth\"", "Runtime OpenAPI protected route auth marker");
                    AssertHelper.IsTrue(securitySchemes.TryGetProperty("BearerAuth", out bearerAuth), "OpenAPI BearerAuth scheme");
                });

                await ExecuteTestAsync("PublicAssistantDocuments.Disabled_Returns403", async () =>
                {
                    Assistant assistant = await server.Database.Assistant.CreateAsync(new Assistant
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = server.DefaultUserId,
                        Name = "Disabled Attachments Assistant",
                        Active = true
                    }).ConfigureAwait(false);

                    await server.Database.AssistantSettings.CreateAsync(new AssistantSettings
                    {
                        AssistantId = assistant.Id,
                        CollectionId = "col_disabled",
                        EnableDocumentAttachments = false
                    }).ConfigureAwait(false);

                    using HttpClient noAuthClient = new HttpClient();
                    noAuthClient.BaseAddress = new Uri(server.BaseUrl);

                    HttpResponseMessage resp = await noAuthClient.GetAsync($"/v1.0/assistants/{assistant.Id}/documents");
                    AssertHelper.AreEqual((int)HttpStatusCode.Forbidden, (int)resp.StatusCode, "disabled attachments route should return 403");
                });

                await ExecuteTestAsync("PublicAssistantDocuments.List_ReturnsSafeCompletedCollectionDocuments", async () =>
                {
                    const string collectionId = "col_public_docs";

                    Assistant assistant = await server.Database.Assistant.CreateAsync(new Assistant
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = server.DefaultUserId,
                        Name = "Public Documents Assistant",
                        Active = true
                    }).ConfigureAwait(false);

                    await server.Database.AssistantSettings.CreateAsync(new AssistantSettings
                    {
                        AssistantId = assistant.Id,
                        CollectionId = collectionId,
                        EnableDocumentAttachments = true,
                        ExposeDocumentSourceUrls = false
                    }).ConfigureAwait(false);

                    AssistantDocument included = await server.Database.AssistantDocument.CreateAsync(new AssistantDocument
                    {
                        TenantId = server.DefaultTenantId,
                        Name = "Guide",
                        OriginalFilename = "guide.pdf",
                        ContentType = "application/pdf",
                        SizeBytes = 1234,
                        BucketName = "secret-bucket",
                        S3Key = "private/guide.pdf",
                        CollectionId = collectionId,
                        Status = DocumentStatusEnum.Completed,
                        SourceUrl = "https://source.example/guide.pdf"
                    }).ConfigureAwait(false);

                    AssistantDocument pending = await server.Database.AssistantDocument.CreateAsync(new AssistantDocument
                    {
                        TenantId = server.DefaultTenantId,
                        Name = "Pending Guide",
                        OriginalFilename = "pending-guide.pdf",
                        ContentType = "application/pdf",
                        BucketName = "secret-bucket",
                        S3Key = "private/pending.pdf",
                        CollectionId = collectionId,
                        Status = DocumentStatusEnum.Pending
                    }).ConfigureAwait(false);

                    AssistantDocument textDocument = await server.Database.AssistantDocument.CreateAsync(new AssistantDocument
                    {
                        TenantId = server.DefaultTenantId,
                        Name = "Notes",
                        OriginalFilename = "notes.txt",
                        ContentType = "text/plain",
                        BucketName = "secret-bucket",
                        S3Key = "private/notes.txt",
                        CollectionId = collectionId,
                        Status = DocumentStatusEnum.Completed
                    }).ConfigureAwait(false);

                    AssistantDocument otherCollection = await server.Database.AssistantDocument.CreateAsync(new AssistantDocument
                    {
                        TenantId = server.DefaultTenantId,
                        Name = "Other Guide",
                        OriginalFilename = "other-guide.pdf",
                        ContentType = "application/pdf",
                        BucketName = "secret-bucket",
                        S3Key = "private/other.pdf",
                        CollectionId = "col_other",
                        Status = DocumentStatusEnum.Completed
                    }).ConfigureAwait(false);

                    using HttpClient noAuthClient = new HttpClient();
                    noAuthClient.BaseAddress = new Uri(server.BaseUrl);

                    HttpResponseMessage resp = await noAuthClient.GetAsync($"/v1.0/assistants/{assistant.Id}/documents?maxResults=100&query=guide");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "public documents route should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(body, included.Id, "completed document included");
                    AssertHelper.StringContains(body, "guide.pdf", "completed document filename included");
                    AssertHelper.IsFalse(body.Contains(pending.Id), "pending document excluded");
                    AssertHelper.IsFalse(body.Contains(otherCollection.Id), "other collection document excluded");
                    AssertHelper.IsFalse(body.Contains("S3Key"), "S3 key field hidden");
                    AssertHelper.IsFalse(body.Contains("BucketName"), "bucket field hidden");
                    AssertHelper.IsFalse(body.Contains("private/guide.pdf"), "S3 object path hidden");
                    AssertHelper.IsFalse(body.Contains("source.example"), "source URL value hidden when disabled");

                    using JsonDocument document = JsonDocument.Parse(body);
                    JsonElement objects = document.RootElement.GetProperty("Objects");
                    AssertHelper.AreEqual(1, objects.GetArrayLength(), "one selectable document");

                    HttpResponseMessage textResp = await noAuthClient.GetAsync($"/v1.0/assistants/{assistant.Id}/documents?maxResults=100&contentType=text/*");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)textResp.StatusCode, "public documents content-type filter should return 200");
                    string textBody = await textResp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(textBody, textDocument.Id, "text document included by content type filter");
                    AssertHelper.IsFalse(textBody.Contains(included.Id), "PDF document excluded by content type filter");
                });

                await ExecuteTestAsync("AssistantToolCalls.CrudFiltersAndDeletes_RoundTripThroughHttp", async () =>
                {
                    Assistant assistant = await server.Database.Assistant.CreateAsync(new Assistant
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = server.DefaultUserId,
                        Name = "Tool Trace Assistant",
                        Active = true
                    }).ConfigureAwait(false);

                    Assistant otherAssistant = await server.Database.Assistant.CreateAsync(new Assistant
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = server.DefaultUserId,
                        Name = "Other Tool Trace Assistant",
                        Active = true
                    }).ConfigureAwait(false);

                    DateTime now = DateTime.UtcNow;
                    AssistantToolCallRecord target = await server.Database.AssistantToolCall.CreateAsync(new AssistantToolCallRecord
                    {
                        TenantId = server.DefaultTenantId,
                        AssistantId = assistant.Id,
                        ChatHistoryId = "chathist_tool_trace_target",
                        RequestHistoryId = "req_tool_trace_target",
                        TraceId = "trace_tool_trace_target",
                        ThreadId = "thread_tool_trace_target",
                        Origin = "web",
                        Iteration = 1,
                        SequenceNumber = 1,
                        ProviderToolCallId = "call_tool_trace_target",
                        ToolName = "collection_search",
                        ArgumentsJson = "{\"query\":\"alpha\",\"api_key\":\"[redacted]\"}",
                        OutputJson = "{\"success\":true,\"result_count\":2}",
                        Success = true,
                        Denied = false,
                        Truncated = false,
                        OutputCharacters = 36,
                        DurationMs = 12.5,
                        StartedUtc = now.AddMilliseconds(-20),
                        FinishedUtc = now,
                        CreatedUtc = now
                    }).ConfigureAwait(false);

                    AssistantToolCallRecord filteredOutByTool = await server.Database.AssistantToolCall.CreateAsync(new AssistantToolCallRecord
                    {
                        TenantId = server.DefaultTenantId,
                        AssistantId = assistant.Id,
                        ChatHistoryId = "chathist_tool_trace_other_tool",
                        RequestHistoryId = "req_tool_trace_other_tool",
                        TraceId = "trace_tool_trace_other_tool",
                        ThreadId = "thread_tool_trace_other_tool",
                        ToolName = "s3_object_read",
                        ArgumentsJson = "{\"bucket\":\"docs\"}",
                        OutputJson = "{\"success\":true}",
                        Success = true,
                        CreatedUtc = now.AddSeconds(-1)
                    }).ConfigureAwait(false);

                    AssistantToolCallRecord filteredOutByAssistant = await server.Database.AssistantToolCall.CreateAsync(new AssistantToolCallRecord
                    {
                        TenantId = server.DefaultTenantId,
                        AssistantId = otherAssistant.Id,
                        ChatHistoryId = "chathist_tool_trace_other_assistant",
                        RequestHistoryId = "req_tool_trace_other_assistant",
                        TraceId = "trace_tool_trace_target",
                        ThreadId = "thread_tool_trace_target",
                        ToolName = "collection_search",
                        ArgumentsJson = "{\"query\":\"alpha\"}",
                        OutputJson = "{\"success\":true}",
                        Success = true,
                        CreatedUtc = now.AddSeconds(-2)
                    }).ConfigureAwait(false);

                    HttpResponseMessage listResp = await server.Client.GetAsync($"/v1.0/assistants/{assistant.Id}/tool-calls?maxResults=10&traceId=trace_tool_trace_target&toolName=collection_search&success=true");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)listResp.StatusCode, "list assistant tool calls should return 200");

                    string listBody = await listResp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(listBody, target.Id, "target trace listed");
                    AssertHelper.StringContains(listBody, "collection_search", "target tool listed");
                    AssertHelper.StringContains(listBody, "[redacted]", "redacted argument retained");
                    AssertHelper.IsFalse(listBody.Contains("secret-key"), "raw secret not present in list response");
                    AssertHelper.IsFalse(listBody.Contains(filteredOutByTool.Id), "different tool filtered out");
                    AssertHelper.IsFalse(listBody.Contains(filteredOutByAssistant.Id), "different assistant filtered out");

                    using (JsonDocument listDocument = JsonDocument.Parse(listBody))
                    {
                        JsonElement objects = listDocument.RootElement.GetProperty("Objects");
                        AssertHelper.AreEqual(1, objects.GetArrayLength(), "filtered tool-call list count");
                        AssertHelper.AreEqual(target.Id, objects[0].GetProperty("Id").GetString(), "filtered tool-call object id");
                    }

                    HttpResponseMessage paginatedResp = await server.Client.GetAsync($"/v1.0/assistants/{assistant.Id}/tool-calls?maxResults=1");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)paginatedResp.StatusCode, "paginated assistant tool calls should return 200");
                    string paginatedBody = await paginatedResp.Content.ReadAsStringAsync();
                    using (JsonDocument paginatedDocument = JsonDocument.Parse(paginatedBody))
                    {
                        AssertHelper.AreEqual(1, paginatedDocument.RootElement.GetProperty("MaxResults").GetInt32(), "paginated tool-call max results");
                        AssertHelper.AreEqual(1, paginatedDocument.RootElement.GetProperty("Objects").GetArrayLength(), "paginated tool-call object count");
                        AssertHelper.IsTrue(paginatedDocument.RootElement.GetProperty("TotalRecords").GetInt64() >= 2, "paginated tool-call total records");
                    }

                    HttpResponseMessage getResp = await server.Client.GetAsync($"/v1.0/assistants/{assistant.Id}/tool-calls/{target.Id}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)getResp.StatusCode, "get assistant tool call should return 200");

                    string getBody = await getResp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(getBody, target.Id, "target trace read by id");
                    AssertHelper.StringContains(getBody, "req_tool_trace_target", "request history id read by id");
                    AssertHelper.StringContains(getBody, "trace_tool_trace_target", "trace id read by id");
                    AssertHelper.StringContains(getBody, "[redacted]", "redacted argument read by id");

                    UserMaster nonAdminUser = await server.Database.User.CreateAsync(new UserMaster
                    {
                        TenantId = server.DefaultTenantId,
                        FirstName = "Trace",
                        LastName = "Reader",
                        Email = "trace-reader@test.local",
                        Active = true,
                        IsAdmin = false,
                        IsTenantAdmin = false
                    }).ConfigureAwait(false);

                    Credential nonAdminCredential = await server.Database.Credential.CreateAsync(new Credential
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = nonAdminUser.Id,
                        Active = true,
                        BearerToken = "test-non-admin-tool-trace"
                    }).ConfigureAwait(false);

                    using (HttpClient nonAdminClient = new HttpClient())
                    {
                        nonAdminClient.BaseAddress = new Uri(server.BaseUrl);
                        nonAdminClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + nonAdminCredential.BearerToken);
                        HttpResponseMessage nonAdminListResp = await nonAdminClient.GetAsync($"/v1.0/assistants/{assistant.Id}/tool-calls");
                        AssertHelper.AreEqual((int)HttpStatusCode.Forbidden, (int)nonAdminListResp.StatusCode, "non-admin tool-call list should return 403");
                    }

                    HttpResponseMessage crossAssistantGetResp = await server.Client.GetAsync($"/v1.0/assistants/{assistant.Id}/tool-calls/{filteredOutByAssistant.Id}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)crossAssistantGetResp.StatusCode, "get other assistant tool call through target assistant route should return 404");

                    HttpResponseMessage crossAssistantDeleteResp = await server.Client.DeleteAsync($"/v1.0/assistants/{assistant.Id}/tool-calls/{filteredOutByAssistant.Id}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)crossAssistantDeleteResp.StatusCode, "delete other assistant tool call through target assistant route should return 404");

                    HttpResponseMessage deleteResp = await server.Client.DeleteAsync($"/v1.0/assistants/{assistant.Id}/tool-calls/{target.Id}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NoContent, (int)deleteResp.StatusCode, "delete assistant tool call should return 204");

                    HttpResponseMessage getAfterDeleteResp = await server.Client.GetAsync($"/v1.0/assistants/{assistant.Id}/tool-calls/{target.Id}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)getAfterDeleteResp.StatusCode, "deleted assistant tool call should return 404");

                    HttpResponseMessage bulkDeleteResp = await server.Client.DeleteAsync($"/v1.0/assistants/{assistant.Id}/tool-calls?toolName=s3_object_read");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)bulkDeleteResp.StatusCode, "bulk delete assistant tool calls should return 200");

                    string bulkDeleteBody = await bulkDeleteResp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(bulkDeleteBody, "DeletedCount", "bulk delete response count field");
                    AssertHelper.StringContains(bulkDeleteBody, "1", "bulk delete response deleted count");

                    HttpResponseMessage getBulkDeletedResp = await server.Client.GetAsync($"/v1.0/assistants/{assistant.Id}/tool-calls/{filteredOutByTool.Id}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)getBulkDeletedResp.StatusCode, "bulk-deleted assistant tool call should return 404");

                    AssistantToolCallRecord expired = await server.Database.AssistantToolCall.CreateAsync(new AssistantToolCallRecord
                    {
                        TenantId = server.DefaultTenantId,
                        AssistantId = assistant.Id,
                        ToolName = "collection_search",
                        ArgumentsJson = "{\"query\":\"expired\"}",
                        OutputJson = "{\"success\":true}",
                        Success = true,
                        CreatedUtc = now.AddDays(-10)
                    }).ConfigureAwait(false);

                    AssistantToolCallRecord retained = await server.Database.AssistantToolCall.CreateAsync(new AssistantToolCallRecord
                    {
                        TenantId = server.DefaultTenantId,
                        AssistantId = assistant.Id,
                        ToolName = "collection_search",
                        ArgumentsJson = "{\"query\":\"retained\"}",
                        OutputJson = "{\"success\":true}",
                        Success = true,
                        CreatedUtc = now
                    }).ConfigureAwait(false);

                    await server.Database.AssistantToolCall.DeleteExpiredAsync(7).ConfigureAwait(false);
                    AssertHelper.IsNull(await server.Database.AssistantToolCall.ReadAsync(expired.Id).ConfigureAwait(false), "expired tool-call record pruned");
                    AssertHelper.IsNotNull(await server.Database.AssistantToolCall.ReadAsync(retained.Id).ConfigureAwait(false), "retained tool-call record remains");
                });

                await ExecuteTestAsync("AssistantToolPolicy.AdminCanUpdateAndListEffectiveTools", async () =>
                {
                    Assistant assistant = await server.Database.Assistant.CreateAsync(new Assistant
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = server.DefaultUserId,
                        Name = "Tool Policy Admin Assistant",
                        Active = true
                    }).ConfigureAwait(false);

                    await server.Database.AssistantSettings.CreateAsync(new AssistantSettings
                    {
                        AssistantId = assistant.Id,
                        CollectionId = "col_tool_policy_admin"
                    }).ConfigureAwait(false);

                    string settingsJson = JsonSerializer.Serialize(new
                    {
                        AssistantId = assistant.Id,
                        CollectionId = "col_tool_policy_admin",
                        InferenceEndpointId = "endpoint_tool_policy_admin",
                        ToolPolicyJson = "{\"EnableToolCalls\":true,\"EnableCollectionEnumerateDocumentsTool\":true,\"MaxSearchResultsPerCall\":4}"
                    });

                    HttpResponseMessage updateResp = await server.Client.PutAsync(
                        $"/v1.0/assistants/{assistant.Id}/settings",
                        new StringContent(settingsJson, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)updateResp.StatusCode, "admin tool policy update");

                    string updateBody = await updateResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using (JsonDocument updateDocument = JsonDocument.Parse(updateBody))
                    {
                        JsonElement policy = updateDocument.RootElement.GetProperty("ToolPolicy");
                        AssertHelper.AreEqual(true, policy.GetProperty("EnableToolCalls").GetBoolean(), "updated settings tool policy");
                        AssertHelper.AreEqual(true, policy.GetProperty("EnableCollectionEnumerateDocumentsTool").GetBoolean(), "updated settings collection enumerate policy");
                        AssertHelper.AreEqual(4, policy.GetProperty("MaxSearchResultsPerCall").GetInt32(), "updated settings max search results");
                    }

                    HttpResponseMessage toolsResp = await server.Client.GetAsync($"/v1.0/assistants/{assistant.Id}/tools").ConfigureAwait(false);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)toolsResp.StatusCode, "effective tools route");
                    string toolsBody = await toolsResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertHelper.StringContains(toolsBody, "collection_enumerate_documents", "effective tool listed");
                    using (JsonDocument toolsDocument = JsonDocument.Parse(toolsBody))
                    {
                        JsonElement descriptor = default;
                        foreach (JsonElement item in toolsDocument.RootElement.EnumerateArray())
                        {
                            if (item.GetProperty("ToolName").GetString() == "collection_enumerate_documents")
                            {
                                descriptor = item;
                                break;
                            }
                        }

                        AssertHelper.AreEqual(JsonValueKind.Object, descriptor.ValueKind, "collection enumerate descriptor found");
                        AssertHelper.AreEqual(true, descriptor.GetProperty("Available").GetBoolean(), "effective tool available");
                    }
                });

                await ExecuteTestAsync("AssistantToolPolicy.ValidateReturnsStableErrorCodes", async () =>
                {
                    Assistant assistant = await server.Database.Assistant.CreateAsync(new Assistant
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = server.DefaultUserId,
                        Name = "Tool Policy Validation Assistant",
                        Active = true
                    }).ConfigureAwait(false);

                    await server.Database.AssistantSettings.CreateAsync(new AssistantSettings
                    {
                        AssistantId = assistant.Id,
                        CollectionId = null
                    }).ConfigureAwait(false);

                    string invalidJsonPayload = JsonSerializer.Serialize(new
                    {
                        ToolPolicyJson = "{not-json"
                    });
                    HttpResponseMessage invalidJsonResp = await server.Client.PostAsync(
                        $"/v1.0/assistants/{assistant.Id}/settings/tools/validate",
                        new StringContent(invalidJsonPayload, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)invalidJsonResp.StatusCode, "invalid policy JSON validation status");
                    string invalidJsonBody = await invalidJsonResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using (JsonDocument invalidJsonDocument = JsonDocument.Parse(invalidJsonBody))
                    {
                        AssertHelper.IsFalse(invalidJsonDocument.RootElement.GetProperty("Success").GetBoolean(), "invalid policy JSON success");
                        AssertHelper.StringContains(invalidJsonBody, "invalid_tool_policy_json", "invalid policy JSON error code");
                    }

                    string noAvailablePayload = JsonSerializer.Serialize(new
                    {
                        ToolPolicyJson = "{\"EnableToolCalls\":true,\"EnableWebSearchTool\":true}"
                    });
                    HttpResponseMessage noAvailableResp = await server.Client.PostAsync(
                        $"/v1.0/assistants/{assistant.Id}/settings/tools/validate",
                        new StringContent(noAvailablePayload, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)noAvailableResp.StatusCode, "no available tools validation status");
                    string noAvailableBody = await noAvailableResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using (JsonDocument noAvailableDocument = JsonDocument.Parse(noAvailableBody))
                    {
                        AssertHelper.IsFalse(noAvailableDocument.RootElement.GetProperty("Success").GetBoolean(), "no available tools success");
                        AssertHelper.StringContains(noAvailableBody, "no_available_tools", "no available tools error code");
                    }

                    HttpResponseMessage diagnosticsResp = await server.Client.PostAsync(
                        $"/v1.0/assistants/{assistant.Id}/settings/tools/test",
                        new StringContent(noAvailablePayload, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)diagnosticsResp.StatusCode, "tool diagnostics status");
                    string diagnosticsBody = await diagnosticsResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using (JsonDocument diagnosticsDocument = JsonDocument.Parse(diagnosticsBody))
                    {
                        AssertHelper.IsFalse(diagnosticsDocument.RootElement.GetProperty("Success").GetBoolean(), "tool diagnostics success");
                        AssertHelper.StringContains(diagnosticsBody, "completion_endpoint_missing", "tool diagnostics endpoint missing code");
                        AssertHelper.StringContains(diagnosticsBody, "no_available_tools", "tool diagnostics validation code");
                    }
                });

                await ExecuteTestAsync("AssistantToolPolicy.NonOwnerCannotUpdateAndToolsRequireAuth", async () =>
                {
                    Assistant assistant = await server.Database.Assistant.CreateAsync(new Assistant
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = server.DefaultUserId,
                        Name = "Tool Policy Owner Assistant",
                        Active = true
                    }).ConfigureAwait(false);

                    await server.Database.AssistantSettings.CreateAsync(new AssistantSettings
                    {
                        AssistantId = assistant.Id,
                        CollectionId = "col_tool_policy_owner"
                    }).ConfigureAwait(false);

                    UserMaster regularUser = new UserMaster
                    {
                        TenantId = server.DefaultTenantId,
                        FirstName = "Regular",
                        LastName = "User",
                        Email = "regular-tool-policy@test.local",
                        Active = true,
                        IsAdmin = false,
                        IsTenantAdmin = false
                    };
                    regularUser.SetPassword("testpassword123");
                    regularUser = await server.Database.User.CreateAsync(regularUser).ConfigureAwait(false);

                    Credential credential = await server.Database.Credential.CreateAsync(new Credential
                    {
                        TenantId = server.DefaultTenantId,
                        UserId = regularUser.Id,
                        Active = true,
                        BearerToken = "test-regular-tool-policy-" + Guid.NewGuid().ToString("N").Substring(0, 8)
                    }).ConfigureAwait(false);

                    using HttpClient regularClient = new HttpClient();
                    regularClient.BaseAddress = new Uri(server.BaseUrl);
                    regularClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + credential.BearerToken);

                    string settingsJson = JsonSerializer.Serialize(new
                    {
                        AssistantId = assistant.Id,
                        CollectionId = "col_tool_policy_owner",
                        InferenceEndpointId = "endpoint_tool_policy_owner",
                        ToolPolicyJson = "{\"EnableToolCalls\":true,\"EnableCollectionSearchTool\":true}"
                    });

                    HttpResponseMessage deniedUpdate = await regularClient.PutAsync(
                        $"/v1.0/assistants/{assistant.Id}/settings",
                        new StringContent(settingsJson, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                    AssertHelper.AreEqual((int)HttpStatusCode.Forbidden, (int)deniedUpdate.StatusCode, "non-owner tool policy update denied");

                    using HttpClient noAuthClient = new HttpClient();
                    noAuthClient.BaseAddress = new Uri(server.BaseUrl);
                    HttpResponseMessage unauthTools = await noAuthClient.GetAsync($"/v1.0/assistants/{assistant.Id}/tools").ConfigureAwait(false);
                    AssertHelper.AreEqual((int)HttpStatusCode.Unauthorized, (int)unauthTools.StatusCode, "effective tools require auth");
                });

                await ExecuteTestAsync("Server.InvalidToken_Returns401", async () =>
                {
                    using HttpClient badClient = new HttpClient();
                    badClient.BaseAddress = new Uri(server.BaseUrl);
                    badClient.DefaultRequestHeaders.Add("Authorization", "Bearer invalid-token-xyz");

                    HttpResponseMessage resp = await badClient.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.Unauthorized, (int)resp.StatusCode, "invalid token should return 401");
                });

                await ExecuteTestAsync("CrawlPlan.DraftConnectivity_UsesSuppliedRepositorySettings", async () =>
                {
                    Dictionary<string, object> repositorySettings = new Dictionary<string, object>
                    {
                        { "RepositoryType", "Web" },
                        { "AuthenticationType", "None" },
                        { "StartUrl", server.BaseUrl + "/" },
                        { "FollowLinks", false },
                        { "FollowRedirects", true },
                        { "ExtractSitemapLinks", false },
                        { "RestrictToChildUrls", true },
                        { "RestrictToSubdomain", true },
                        { "RestrictToRootDomain", true },
                        { "IgnoreRobotsTxt", true },
                        { "UseHeadlessBrowser", false },
                        { "MaxDepth", 1 },
                        { "MaxParallelTasks", 1 },
                        { "CrawlDelayMs", 0 }
                    };

                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Name", "Draft Connectivity Probe" },
                        { "RepositoryType", "Web" },
                        { "RepositorySettings", repositorySettings }
                    };

                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/crawlplans/connectivity", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "draft connectivity should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(body, "Success", "draft connectivity response success field");
                    AssertHelper.StringContains(body, "Message", "draft connectivity response message field");
                });

                await ExecuteTestAsync("Server.WhoAmI_ReturnsAuthenticatedUser", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/whoami");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "whoami should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("admin@test.local"), "whoami should contain admin email");
                });

                // ===== AuthenticationFlowTests =====

                await ExecuteTestAsync("Auth.PostAuthenticate_ValidCredentials", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Email", "admin@test.local" },
                        { "Password", "testpassword123" },
                        { "TenantId", server.DefaultTenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "valid auth should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("BearerToken") || body.Contains("bearerToken") || body.Contains("bearer_token"),
                        "response should contain bearer token field");
                });

                await ExecuteTestAsync("Auth.PostAuthenticate_InvalidPassword", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Email", "admin@test.local" },
                        { "Password", "wrongpassword" },
                        { "TenantId", server.DefaultTenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 401 || (int)resp.StatusCode == 400,
                        "invalid password should return 401 or 400, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Auth.PostAuthenticate_NonExistentEmail", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Email", "nonexistent@test.local" },
                        { "Password", "password123" },
                        { "TenantId", server.DefaultTenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 401 || (int)resp.StatusCode == 400,
                        "non-existent email should return 401 or 400, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Auth.BearerToken_SubsequentRequest", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Email", "admin@test.local" },
                        { "Password", "testpassword123" },
                        { "TenantId", server.DefaultTenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage authResp = await server.Client.PostAsync("/v1.0/authenticate", content);
                    string authBody = await authResp.Content.ReadAsStringAsync();

                    using HttpClient tokenClient = new HttpClient();
                    tokenClient.BaseAddress = new Uri(server.BaseUrl);
                    tokenClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {server.AdminBearerToken}");

                    HttpResponseMessage resp = await tokenClient.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "authenticated request should succeed");
                });

                // ===== CrudLifecycleTests =====

                string tenantId = server.DefaultTenantId;
                string createdAssistantId = null;

                await ExecuteTestAsync("CRUD.Assistant.Create", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Name", "Integration Test Assistant" },
                        { "TenantId", tenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create assistant should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("asst_"), "response should contain assistant ID with asst_ prefix");

                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdAssistantId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdAssistantId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdAssistantId, "should extract assistant ID from response");
                });

                await ExecuteTestAsync("CRUD.Assistant.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read assistant should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("Integration Test Assistant"), "response should contain assistant name");
                });

                await ExecuteTestAsync("CRUD.Assistant.Read_NotFound", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants/asst_nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent assistant should return 404");
                });

                await ExecuteTestAsync("CRUD.Assistant.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate assistants should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Length > 2, "response should contain data");
                });

                await ExecuteTestAsync("CRUD.Assistant.Update", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpResponseMessage readResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    string readBody = await readResp.Content.ReadAsStringAsync();

                    using JsonDocument doc = JsonDocument.Parse(readBody);
                    string updatedJson = readBody.Replace("Integration Test Assistant", "Updated Assistant Name");
                    HttpContent content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/assistants/{createdAssistantId}", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode,
                        "update assistant should return 200, got " + (int)resp.StatusCode);

                    HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    string verifyBody = await verifyResp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(verifyBody.Contains("Updated Assistant Name"), "updated name should be persisted");
                });

                await ExecuteTestAsync("CRUD.Assistant.Head_Exists", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, $"/v1.0/assistants/{createdAssistantId}");
                    HttpResponseMessage resp = await server.Client.SendAsync(req);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "HEAD existing assistant should return 200");
                });

                await ExecuteTestAsync("CRUD.Assistant.Head_NotFound", async () =>
                {
                    HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, "/v1.0/assistants/asst_nonexistent");
                    HttpResponseMessage resp = await server.Client.SendAsync(req);
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "HEAD non-existent should return 404");
                });

                await ExecuteTestAsync("CRUD.Assistant.Delete", async () =>
                {
                    AssertHelper.IsNotNull(createdAssistantId, "assistant ID should exist from create test");

                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/assistants/{createdAssistantId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete should return 200 or 204, got " + (int)resp.StatusCode);

                    HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/assistants/{createdAssistantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)verifyResp.StatusCode, "deleted assistant should not be found");
                });

                // --- User CRUD lifecycle ---

                string createdUserId = null;

                await ExecuteTestAsync("CRUD.User.Create", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "FirstName", "Test" },
                        { "LastName", "User" },
                        { "Email", "testuser@integration.local" },
                        { "Password", "password123" },
                        { "TenantId", tenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/tenants/{tenantId}/users", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create user should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdUserId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdUserId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdUserId, "should extract user ID from response");
                });

                await ExecuteTestAsync("CRUD.User.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdUserId, "user ID should exist");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/users/{createdUserId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read user should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("testuser@integration.local"), "response should contain user email");
                    AssertHelper.IsTrue(!body.Contains("password123"), "password should not be in response body");
                });

                await ExecuteTestAsync("CRUD.User.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/users");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate users should return 200");
                });

                await ExecuteTestAsync("CRUD.User.Delete", async () =>
                {
                    AssertHelper.IsNotNull(createdUserId, "user ID should exist");

                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{tenantId}/users/{createdUserId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete user should return 200 or 204, got " + (int)resp.StatusCode);
                });

                // --- Ingestion Rule CRUD ---

                string createdRuleId = null;

                await ExecuteTestAsync("CRUD.IngestionRule.Create", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "TenantId", tenantId },
                        { "Name", "Test Rule" }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/ingestion-rules", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create rule should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdRuleId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdRuleId = idElem2.GetString();
                });

                await ExecuteTestAsync("CRUD.IngestionRule.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/ingestion-rules");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate rules should return 200");
                });

                await ExecuteTestAsync("CRUD.IngestionRule.Delete", async () =>
                {
                    if (createdRuleId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/ingestion-rules/{createdRuleId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete rule should return 200 or 204, got " + (int)resp.StatusCode);
                    }
                });

                // --- Tenant CRUD lifecycle ---

                string createdTenantId = null;

                await ExecuteTestAsync("CRUD.Tenant.Create", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Name", "Integration CRUD Tenant" },
                        { "Active", true }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/tenants", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create tenant should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Tenant", out JsonElement tenantElem))
                    {
                        if (tenantElem.TryGetProperty("Id", out JsonElement idElem))
                            createdTenantId = idElem.GetString();
                        else if (tenantElem.TryGetProperty("id", out JsonElement idElem2))
                            createdTenantId = idElem2.GetString();
                    }
                    else if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdTenantId = idElem.GetString();

                    AssertHelper.IsNotNull(createdTenantId, "should extract tenant ID");
                    AssertHelper.IsTrue(createdTenantId.StartsWith("ten_"), "tenant ID should have ten_ prefix");
                });

                await ExecuteTestAsync("CRUD.Tenant.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{createdTenantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read tenant should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("Integration CRUD Tenant"), "response should contain tenant name");
                });

                await ExecuteTestAsync("CRUD.Tenant.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate tenants should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Length > 2, "response should contain data");
                });

                await ExecuteTestAsync("CRUD.Tenant.Head_Exists", async () =>
                {
                    AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                    HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, $"/v1.0/tenants/{createdTenantId}");
                    HttpResponseMessage resp = await server.Client.SendAsync(req);
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "HEAD existing tenant should return 200");
                });

                await ExecuteTestAsync("CRUD.Tenant.Delete", async () =>
                {
                    AssertHelper.IsNotNull(createdTenantId, "tenant ID should exist");

                    HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{createdTenantId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                        "delete tenant should return 200 or 204, got " + (int)resp.StatusCode);

                    HttpResponseMessage verifyResp = await server.Client.GetAsync($"/v1.0/tenants/{createdTenantId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)verifyResp.StatusCode, "deleted tenant should return 404");
                });

                // --- Credential CRUD lifecycle ---

                string createdCredentialId = null;

                await ExecuteTestAsync("CRUD.Credential.Create", async () =>
                {
                    Dictionary<string, object> userPayload = new Dictionary<string, object>
                    {
                        { "FirstName", "Cred" },
                        { "LastName", "TestUser" },
                        { "Email", "credtest@integration.local" },
                        { "Password", "password123" },
                        { "TenantId", tenantId }
                    };
                    string userJson = JsonSerializer.Serialize(userPayload);
                    HttpContent userContent = new StringContent(userJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage userResp = await server.Client.PutAsync($"/v1.0/tenants/{tenantId}/users", userContent);
                    string userBody = await userResp.Content.ReadAsStringAsync();

                    string credUserId = null;
                    using (JsonDocument userDoc = JsonDocument.Parse(userBody))
                    {
                        if (userDoc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                            credUserId = idElem.GetString();
                        else if (userDoc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                            credUserId = idElem2.GetString();
                    }

                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "TenantId", tenantId },
                        { "UserId", credUserId },
                        { "Active", true }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/tenants/{tenantId}/credentials", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create credential should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement credIdElem))
                        createdCredentialId = credIdElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement credIdElem2))
                        createdCredentialId = credIdElem2.GetString();

                    AssertHelper.IsNotNull(createdCredentialId, "should extract credential ID");
                });

                await ExecuteTestAsync("CRUD.Credential.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/tenants/{tenantId}/credentials");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate credentials should return 200");
                });

                await ExecuteTestAsync("CRUD.Credential.Delete", async () =>
                {
                    if (createdCredentialId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/tenants/{tenantId}/credentials/{createdCredentialId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete credential should return 200 or 204, got " + (int)resp.StatusCode);
                    }
                });

                // --- Assistant Settings lifecycle ---

                string settingsAssistantId = null;

                await ExecuteTestAsync("CRUD.Settings.CreateAssistant", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Name", "Settings Test Assistant" },
                        { "TenantId", tenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        settingsAssistantId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        settingsAssistantId = idElem2.GetString();

                    AssertHelper.IsNotNull(settingsAssistantId, "should extract assistant ID for settings tests");
                });

                await ExecuteTestAsync("CRUD.Settings.Put", async () =>
                {
                    AssertHelper.IsNotNull(settingsAssistantId, "assistant ID for settings");

                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "AssistantId", settingsAssistantId },
                        { "Temperature", 0.8 },
                        { "InferenceEndpointId", "ep_test_inference" },
                        { "LoadModelsOnChatOpen", true },
                        { "EnableReranking", true },
                        { "RerankerTopK", 3 },
                        { "RerankerScoreThreshold", 5.0 },
                        { "EnableDocumentAttachments", true },
                        { "DocumentAttachmentMaxCount", 7 },
                        { "ExposeDocumentSourceUrls", true }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync($"/v1.0/assistants/{settingsAssistantId}/settings", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 200 || (int)resp.StatusCode == 201,
                        "put settings should return 200 or 201, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("CRUD.Settings.Get", async () =>
                {
                    AssertHelper.IsNotNull(settingsAssistantId, "assistant ID for settings");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/assistants/{settingsAssistantId}/settings");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "get settings should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("ep_test_inference"), "response should contain inference endpoint id");
                    AssertHelper.IsTrue(body.Contains("LoadModelsOnChatOpen"), "response should contain LoadModelsOnChatOpen");
                    using JsonDocument settingsDocument = JsonDocument.Parse(body);
                    AssertHelper.AreEqual(true, settingsDocument.RootElement.GetProperty("EnableDocumentAttachments").GetBoolean(), "response EnableDocumentAttachments");
                    AssertHelper.AreEqual(7, settingsDocument.RootElement.GetProperty("DocumentAttachmentMaxCount").GetInt32(), "response DocumentAttachmentMaxCount");
                    AssertHelper.AreEqual(true, settingsDocument.RootElement.GetProperty("ExposeDocumentSourceUrls").GetBoolean(), "response ExposeDocumentSourceUrls");
                });

                await ExecuteTestAsync("CRUD.Settings.Cleanup", async () =>
                {
                    if (settingsAssistantId != null)
                    {
                        await server.Client.DeleteAsync($"/v1.0/assistants/{settingsAssistantId}");
                    }
                });

                // --- Feedback lifecycle ---

                await ExecuteTestAsync("CRUD.Feedback.Enumerate_Empty", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/feedback");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate feedback should return 200");
                });

                await ExecuteTestAsync("CRUD.Feedback.Read_NotFound", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/feedback/afb_nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent feedback should return 404");
                });

                // --- History lifecycle ---

                await ExecuteTestAsync("CRUD.History.Enumerate_Empty", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/history");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate history should return 200");
                });

                await ExecuteTestAsync("CRUD.History.Read_NotFound", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/history/ch_nonexistent");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode, "non-existent history should return 404");
                });

                // --- CrawlPlan CRUD lifecycle ---

                string createdCrawlPlanId = null;
                string createdCifsCrawlPlanId = null;
                string createdNfsCrawlPlanId = null;

                await ExecuteTestAsync("CRUD.CrawlPlan.Create", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "TenantId", tenantId },
                        { "Name", "Integration Test Crawl Plan" },
                        { "RepositoryType", "Web" },
                        {
                            "RepositorySettings",
                            new Dictionary<string, object>
                            {
                                { "RepositoryType", "Web" },
                                { "StartUrl", "https://example.com" }
                            }
                        }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/crawlplans", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create crawl plan should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdCrawlPlanId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdCrawlPlanId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdCrawlPlanId, "should extract crawl plan ID");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Create_CIFS", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "TenantId", tenantId },
                        { "Name", "Integration Test CIFS Crawl Plan" },
                        { "RepositoryType", "CIFS" },
                        {
                            "RepositorySettings",
                            new Dictionary<string, object>
                            {
                                { "RepositoryType", "CIFS" },
                                { "CifsHostname", "fileserver.example.com" },
                                { "CifsUsername", "crawler" },
                                { "CifsPassword", "secret" },
                                { "CifsShareName", "content" },
                                { "IncludeSubdirectories", true }
                            }
                        }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/crawlplans", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create CIFS crawl plan should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdCifsCrawlPlanId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdCifsCrawlPlanId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdCifsCrawlPlanId, "should extract CIFS crawl plan ID");
                    AssertHelper.StringContains(body, "CIFS", "CIFS response repository type");
                    AssertHelper.StringContains(body, "CifsHostname", "CIFS response settings");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Create_NFS", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "TenantId", tenantId },
                        { "Name", "Integration Test NFS Crawl Plan" },
                        { "RepositoryType", "NFS" },
                        {
                            "RepositorySettings",
                            new Dictionary<string, object>
                            {
                                { "RepositoryType", "NFS" },
                                { "NfsHostname", "nfs.example.com" },
                                { "NfsUserId", 1000 },
                                { "NfsGroupId", 1000 },
                                { "NfsShareName", "/exports/content" },
                                { "NfsVersion", "V3" },
                                { "IncludeSubdirectories", true }
                            }
                        }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/crawlplans", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.Created, (int)resp.StatusCode,
                        "create NFS crawl plan should return 201, got " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        createdNfsCrawlPlanId = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        createdNfsCrawlPlanId = idElem2.GetString();

                    AssertHelper.IsNotNull(createdNfsCrawlPlanId, "should extract NFS crawl plan ID");
                    AssertHelper.StringContains(body, "NFS", "NFS response repository type");
                    AssertHelper.StringContains(body, "NfsHostname", "NFS response settings");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Create_InvalidSettings_Returns400", async () =>
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "TenantId", tenantId },
                        { "Name", "Invalid CIFS Crawl Plan" },
                        { "RepositoryType", "CIFS" },
                        {
                            "RepositorySettings",
                            new Dictionary<string, object>
                            {
                                { "RepositoryType", "CIFS" },
                                { "CifsHostname", "fileserver.example.com" },
                                { "CifsUsername", "crawler" },
                                { "CifsShareName", "content" }
                            }
                        }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/crawlplans", content);
                    AssertHelper.AreEqual((int)HttpStatusCode.BadRequest, (int)resp.StatusCode,
                        "invalid CIFS crawl plan should return 400, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Read", async () =>
                {
                    AssertHelper.IsNotNull(createdCrawlPlanId, "crawl plan ID should exist");

                    HttpResponseMessage resp = await server.Client.GetAsync($"/v1.0/crawlplans/{createdCrawlPlanId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "read crawl plan should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains("Integration Test Crawl Plan"), "response should contain crawl plan name");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Read_FileServerTypes", async () =>
                {
                    AssertHelper.IsNotNull(createdCifsCrawlPlanId, "CIFS crawl plan ID should exist");
                    AssertHelper.IsNotNull(createdNfsCrawlPlanId, "NFS crawl plan ID should exist");

                    HttpResponseMessage cifsResp = await server.Client.GetAsync($"/v1.0/crawlplans/{createdCifsCrawlPlanId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)cifsResp.StatusCode, "read CIFS crawl plan should return 200");
                    string cifsBody = await cifsResp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(cifsBody, "CIFS", "CIFS read repository type");
                    AssertHelper.StringContains(cifsBody, "CifsShareName", "CIFS read settings");

                    HttpResponseMessage nfsResp = await server.Client.GetAsync($"/v1.0/crawlplans/{createdNfsCrawlPlanId}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)nfsResp.StatusCode, "read NFS crawl plan should return 200");
                    string nfsBody = await nfsResp.Content.ReadAsStringAsync();
                    AssertHelper.StringContains(nfsBody, "NFS", "NFS read repository type");
                    AssertHelper.StringContains(nfsBody, "NfsShareName", "NFS read settings");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Enumerate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/crawlplans");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "enumerate crawl plans should return 200");
                });

                await ExecuteTestAsync("CRUD.CrawlPlan.Delete", async () =>
                {
                    if (createdCrawlPlanId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/crawlplans/{createdCrawlPlanId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete crawl plan should return 200 or 204, got " + (int)resp.StatusCode);
                    }

                    if (createdCifsCrawlPlanId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/crawlplans/{createdCifsCrawlPlanId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete CIFS crawl plan should return 200 or 204, got " + (int)resp.StatusCode);
                    }

                    if (createdNfsCrawlPlanId != null)
                    {
                        HttpResponseMessage resp = await server.Client.DeleteAsync($"/v1.0/crawlplans/{createdNfsCrawlPlanId}");
                        AssertHelper.IsTrue(
                            (int)resp.StatusCode == 200 || (int)resp.StatusCode == 204,
                            "delete NFS crawl plan should return 200 or 204, got " + (int)resp.StatusCode);
                    }
                });

                // ===== PaginationTests =====

                string[] assistantIds = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    Dictionary<string, object> payload = new Dictionary<string, object>
                    {
                        { "Name", $"Pagination Asst {i}" },
                        { "TenantId", tenantId }
                    };
                    string json = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        assistantIds[i] = idElem.GetString();
                    else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        assistantIds[i] = idElem2.GetString();
                }

                await ExecuteTestAsync("Pagination.MaxResults1_MultiplePages", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=1");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "page 1 should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);

                    bool hasObjects = doc.RootElement.TryGetProperty("Objects", out JsonElement objsElem)
                        || doc.RootElement.TryGetProperty("objects", out objsElem);
                    AssertHelper.IsTrue(hasObjects, "response should have Objects property");
                    AssertHelper.AreEqual(1, objsElem.GetArrayLength(), "page 1 should have exactly 1 item");
                });

                await ExecuteTestAsync("Pagination.ContinuationToken_Works", async () =>
                {
                    HttpResponseMessage resp1 = await server.Client.GetAsync("/v1.0/assistants?maxResults=1");
                    string body1 = await resp1.Content.ReadAsStringAsync();
                    using JsonDocument doc1 = JsonDocument.Parse(body1);

                    string continuationToken = null;
                    if (doc1.RootElement.TryGetProperty("ContinuationToken", out JsonElement ctElem))
                        continuationToken = ctElem.GetString();
                    else if (doc1.RootElement.TryGetProperty("continuationToken", out JsonElement ctElem2))
                        continuationToken = ctElem2.GetString();

                    AssertHelper.IsNotNull(continuationToken, "page 1 should have a continuation token");

                    HttpResponseMessage resp2 = await server.Client.GetAsync($"/v1.0/assistants?maxResults=1&continuationToken={continuationToken}");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp2.StatusCode, "page 2 should return 200");

                    string body2 = await resp2.Content.ReadAsStringAsync();
                    using JsonDocument doc2 = JsonDocument.Parse(body2);

                    bool hasObjects2 = doc2.RootElement.TryGetProperty("Objects", out JsonElement objs2)
                        || doc2.RootElement.TryGetProperty("objects", out objs2);
                    AssertHelper.IsTrue(hasObjects2, "page 2 should have Objects");
                    AssertHelper.AreEqual(1, objs2.GetArrayLength(), "page 2 should have exactly 1 item");

                    string id1 = null, id2 = null;
                    if (doc1.RootElement.TryGetProperty("Objects", out JsonElement o1) || doc1.RootElement.TryGetProperty("objects", out o1))
                    {
                        JsonElement first = o1[0];
                        if (first.TryGetProperty("Id", out JsonElement ie)) id1 = ie.GetString();
                        else if (first.TryGetProperty("id", out JsonElement ie2)) id1 = ie2.GetString();
                    }
                    if (doc2.RootElement.TryGetProperty("Objects", out JsonElement o2) || doc2.RootElement.TryGetProperty("objects", out o2))
                    {
                        JsonElement first = o2[0];
                        if (first.TryGetProperty("Id", out JsonElement ie)) id2 = ie.GetString();
                        else if (first.TryGetProperty("id", out JsonElement ie2)) id2 = ie2.GetString();
                    }
                    AssertHelper.AreNotEqual(id1, id2, "page 1 and page 2 should have different items");
                });

                await ExecuteTestAsync("Pagination.EndOfResults_OnFinalPage", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=100");
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);

                    bool endOfResults = false;
                    if (doc.RootElement.TryGetProperty("EndOfResults", out JsonElement eorElem))
                        endOfResults = eorElem.GetBoolean();
                    else if (doc.RootElement.TryGetProperty("endOfResults", out JsonElement eorElem2))
                        endOfResults = eorElem2.GetBoolean();

                    AssertHelper.IsTrue(endOfResults, "large page should indicate end of results");
                });

                await ExecuteTestAsync("Pagination.TotalRecords_Accurate", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=100");
                    string body = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(body);

                    int totalRecords = 0;
                    if (doc.RootElement.TryGetProperty("TotalRecords", out JsonElement trElem))
                        totalRecords = trElem.GetInt32();
                    else if (doc.RootElement.TryGetProperty("totalRecords", out JsonElement trElem2))
                        totalRecords = trElem2.GetInt32();

                    AssertHelper.IsTrue(totalRecords >= 3, "total records should be at least 3, got " + totalRecords);
                });

                // Cleanup pagination assistants
                foreach (string id in assistantIds)
                {
                    if (id != null)
                        await server.Client.DeleteAsync($"/v1.0/assistants/{id}");
                }

                // ===== MultiTenantIsolationTests =====

                string tenantAId = server.DefaultTenantId;
                string tenantBId = null;
                string tenantBToken = null;

                // Setup: Create tenant B
                Dictionary<string, object> tenantPayload = new Dictionary<string, object>
                {
                    { "Name", "Isolation Tenant B" },
                    { "Active", true }
                };
                string tenantJson = JsonSerializer.Serialize(tenantPayload);
                HttpContent tenantContent = new StringContent(tenantJson, Encoding.UTF8, "application/json");
                HttpResponseMessage tenantResp = await server.Client.PutAsync("/v1.0/tenants", tenantContent);
                string tenantBody = await tenantResp.Content.ReadAsStringAsync();
                using (JsonDocument tenantDoc = JsonDocument.Parse(tenantBody))
                {
                    if (tenantDoc.RootElement.TryGetProperty("Tenant", out JsonElement tenantElem))
                    {
                        if (tenantElem.TryGetProperty("Id", out JsonElement idElem))
                            tenantBId = idElem.GetString();
                        else if (tenantElem.TryGetProperty("id", out JsonElement idElem2))
                            tenantBId = idElem2.GetString();
                    }
                }

                // Setup: Create regular user in tenant B
                if (tenantBId != null)
                {
                    Dictionary<string, object> userPayload = new Dictionary<string, object>
                    {
                        { "FirstName", "Regular" },
                        { "LastName", "UserB" },
                        { "Email", "regular@tenantb.local" },
                        { "Password", "password123" },
                        { "TenantId", tenantBId },
                        { "IsAdmin", false },
                        { "IsTenantAdmin", false }
                    };
                    string userJson = JsonSerializer.Serialize(userPayload);
                    HttpContent userContent = new StringContent(userJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage userResp = await server.Client.PutAsync($"/v1.0/tenants/{tenantBId}/users", userContent);
                    string userBody = await userResp.Content.ReadAsStringAsync();

                    string regularUserId = null;
                    using (JsonDocument userDoc = JsonDocument.Parse(userBody))
                    {
                        if (userDoc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                            regularUserId = idElem.GetString();
                        else if (userDoc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                            regularUserId = idElem2.GetString();
                    }

                    if (regularUserId != null)
                    {
                        Dictionary<string, object> credPayload = new Dictionary<string, object>
                        {
                            { "TenantId", tenantBId },
                            { "UserId", regularUserId },
                            { "Active", true }
                        };
                        string credJson = JsonSerializer.Serialize(credPayload);
                        HttpContent credContent = new StringContent(credJson, Encoding.UTF8, "application/json");
                        HttpResponseMessage credResp = await server.Client.PutAsync($"/v1.0/tenants/{tenantBId}/credentials", credContent);
                        string credBody = await credResp.Content.ReadAsStringAsync();
                        using (JsonDocument credDoc = JsonDocument.Parse(credBody))
                        {
                            if (credDoc.RootElement.TryGetProperty("BearerToken", out JsonElement btElem))
                                tenantBToken = btElem.GetString();
                            else if (credDoc.RootElement.TryGetProperty("bearerToken", out JsonElement btElem2))
                                tenantBToken = btElem2.GetString();
                        }
                    }
                }

                // Create an assistant in tenant A
                string tenantAAssistantId = null;
                Dictionary<string, object> asstAPayload = new Dictionary<string, object>
                {
                    { "Name", "Tenant A Assistant" },
                    { "TenantId", tenantAId }
                };
                string asstAJson = JsonSerializer.Serialize(asstAPayload);
                HttpContent asstAContent = new StringContent(asstAJson, Encoding.UTF8, "application/json");
                HttpResponseMessage asstAResp = await server.Client.PutAsync("/v1.0/assistants", asstAContent);
                string asstABody = await asstAResp.Content.ReadAsStringAsync();
                using (JsonDocument asstADoc = JsonDocument.Parse(asstABody))
                {
                    if (asstADoc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                        tenantAAssistantId = idElem.GetString();
                    else if (asstADoc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                        tenantAAssistantId = idElem2.GetString();
                }

                HttpClient tenantBClient = new HttpClient();
                tenantBClient.BaseAddress = new Uri(server.BaseUrl);
                if (tenantBToken != null)
                    tenantBClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tenantBToken}");

                await ExecuteTestAsync("MultiTenant.TenantB_CannotSee_TenantA_Assistants", async () =>
                {
                    if (tenantBToken == null)
                    {
                        AssertHelper.IsNotNull(tenantBToken, "tenant B token required");
                        return;
                    }

                    HttpResponseMessage resp = await tenantBClient.GetAsync("/v1.0/assistants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "tenant B enumerate should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(!body.Contains("Tenant A Assistant"),
                        "tenant B should not see tenant A's assistants");
                });

                await ExecuteTestAsync("MultiTenant.TenantB_CannotRead_TenantA_Assistant", async () =>
                {
                    if (tenantBToken == null || tenantAAssistantId == null)
                    {
                        AssertHelper.IsNotNull(tenantBToken, "tenant B token required");
                        return;
                    }

                    HttpResponseMessage resp = await tenantBClient.GetAsync($"/v1.0/assistants/{tenantAAssistantId}");
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 404 || (int)resp.StatusCode == 403,
                        "tenant B reading tenant A's assistant should return 403 or 404, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("MultiTenant.GlobalAdmin_CanSee_BothTenants", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/tenants");
                    AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "admin enumerate tenants should return 200");

                    string body = await resp.Content.ReadAsStringAsync();
                    AssertHelper.IsTrue(body.Contains(tenantAId) || body.Contains("Integration Test Tenant"),
                        "admin should see tenant A");
                    if (tenantBId != null)
                    {
                        AssertHelper.IsTrue(body.Contains(tenantBId) || body.Contains("Isolation Tenant B"),
                            "admin should see tenant B");
                    }
                });

                // Cleanup multi-tenant
                tenantBClient.Dispose();
                if (tenantAAssistantId != null)
                    await server.Client.DeleteAsync($"/v1.0/assistants/{tenantAAssistantId}");
                if (tenantBId != null)
                    await server.Client.DeleteAsync($"/v1.0/tenants/{tenantBId}");

                // ===== ErrorHandlingTests =====

                await ExecuteTestAsync("Error.MalformedJson_Returns400", async () =>
                {
                    HttpContent content = new StringContent("{invalid json!!!", Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 400 || (int)resp.StatusCode == 500,
                        "malformed JSON should return 400 or 500, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Error.EmptyBody_Returns400", async () =>
                {
                    HttpContent content = new StringContent("", Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                    AssertHelper.IsTrue(
                        (int)resp.StatusCode == 400 || (int)resp.StatusCode == 500,
                        "empty body should return 400 or 500, got " + (int)resp.StatusCode);
                });

                await ExecuteTestAsync("Error.NonExistentEntity_Returns404", async () =>
                {
                    HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants/asst_does_not_exist_xyz");
                    AssertHelper.AreEqual((int)HttpStatusCode.NotFound, (int)resp.StatusCode,
                        "non-existent entity should return 404");
                });
            }
            finally
            {
                server.Dispose();
            }

            return GetResults();
        }
    }
}
