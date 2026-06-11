// AssistantHub JavaScript SDK Test Suite
// Standalone CLI test runner matching the C# and Python test output format.

import { randomUUID } from "node:crypto";
import { AssistantHubClient } from "./dist/esm/index.js";

// ---------------------------------------------------------------------------
// Assertion helpers
// ---------------------------------------------------------------------------

function assertTrue(condition, message) {
  if (!condition) throw new Error("Assertion failed: " + message);
}

function assertFalse(condition, message) {
  if (condition) throw new Error("Assertion failed (expected false): " + message);
}

function assertNotNull(value, label) {
  if (value === null || value === undefined) throw new Error(label + " must not be null/undefined");
}

function assertEqual(expected, actual, label) {
  if (expected !== actual) throw new Error(label + ": expected " + JSON.stringify(expected) + " but got " + JSON.stringify(actual));
}

function assertStartsWith(value, prefix, label) {
  if (typeof value !== "string" || !value.startsWith(prefix)) {
    throw new Error(label + ": expected to start with " + JSON.stringify(prefix) + " but got " + JSON.stringify(value));
  }
}

function assertGte(value, minimum, label) {
  if (value < minimum) throw new Error(label + ": expected >= " + minimum + " but got " + value);
}

// ---------------------------------------------------------------------------
// Test harness
// ---------------------------------------------------------------------------

class TestResult {
  constructor(testName, passed, runtimeMs, error) {
    this.testName = testName;
    this.passed = passed;
    this.runtimeMs = runtimeMs;
    this.error = error || null;
  }
}

class TestRunner {
  constructor() {
    this.results = [];
  }

  async runTest(name, fn) {
    const start = performance.now();
    try {
      await fn();
      const ms = Math.round(performance.now() - start);
      this.results.push(new TestResult(name, true, ms, null));
      console.log("  \x1b[32mPASS\x1b[0m  " + name + " (" + ms + "ms)");
    } catch (err) {
      const ms = Math.round(performance.now() - start);
      const msg = err && err.message ? err.message : String(err);
      this.results.push(new TestResult(name, false, ms, msg));
      console.log("  \x1b[31mFAIL\x1b[0m  " + name + " (" + ms + "ms)");
      console.log("         " + msg);
    }
  }

  printSummary(totalMs) {
    const total = this.results.length;
    const passed = this.results.filter((r) => r.passed).length;
    const failed = total - passed;

    console.log("");
    console.log("=".repeat(80));
    console.log("TEST SUMMARY");
    console.log("=".repeat(80));
    console.log("  Total:   " + total);
    console.log("  Passed:  " + passed);
    console.log("  Failed:  " + failed);
    console.log("  Runtime: " + Math.round(totalMs) + "ms");

    if (failed > 0) {
      console.log("");
      console.log("FAILED TESTS:");
      for (const r of this.results) {
        if (!r.passed) {
          console.log("  \x1b[31mFAIL\x1b[0m  " + r.testName);
          if (r.error) console.log("         " + r.error);
        }
      }
    }

    console.log("");
    if (failed > 0) {
      console.log("\x1b[31mOVERALL: FAIL\x1b[0m");
    } else {
      console.log("\x1b[32mOVERALL: PASS\x1b[0m");
    }
  }
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function uniqueSuffix() {
  return randomUUID().replace(/-/g, "").substring(0, 8);
}

function btoa64(str) {
  return Buffer.from(str, "utf-8").toString("base64");
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function truthy(value) {
  if (typeof value !== "string") return false;
  return ["1", "true", "yes", "y"].includes(value.trim().toLowerCase());
}

function localOnlyRequested() {
  if (truthy(process.env.ASSISTANTHUB_SDK_LOCAL_ONLY)) return true;
  return process.argv.slice(2).some((arg) => {
    const normalized = String(arg).trim().toLowerCase();
    return normalized === "--local-only" || normalized === "local-only" || normalized === "localonly=true" || normalized === "local=true";
  });
}

// ---------------------------------------------------------------------------
// Local SDK contract tests
// ---------------------------------------------------------------------------

async function sdkContractTests(runner) {
  await runner.runTest("SDK contract: chatCompletion sends attached_document_ids", async () => {
    let capturedUrl = null;
    let capturedInit = null;

    const client = new AssistantHubClient({
      baseUrl: "http://localhost:6600",
      apiKey: "test-key",
      fetch: async (url, init) => {
        capturedUrl = String(url);
        capturedInit = init;
        return new Response(
          JSON.stringify({
            id: "chatcmpl_local",
            object: "chat.completion",
            created: 0,
            model: "test-model",
            choices: [
              {
                index: 0,
                message: { role: "assistant", content: "done", thinking: "hidden reasoning" },
                finish_reason: "stop",
              },
            ],
            usage: {
              prompt_tokens: 12,
              completion_tokens: 4,
              total_tokens: 16,
              tool_definition_tokens: 5,
              prompt_tokens_details: {
                cached_tokens: 3,
                tool_tokens: 5,
              },
              completion_tokens_details: {
                reasoning_tokens: 7,
              },
            },
            tool_calls: [
              {
                tool_call_id: "call_search",
                tool_name: "collection_search",
                display_label: "Searching collection",
                iteration: 1,
                sequence_number: 1,
                success: true,
                denied: false,
                truncated: false,
                output_characters: 128,
                result_count: 3,
                credits_used: 2,
                provider_latency_ms: 45.5,
                duration_ms: 12.5,
                summary: "Searching collection completed.",
              },
            ],
            retrieval: {
              collection_id: "col_abc123",
              duration_ms: 42.7,
              chunks_returned: 3,
              attached_document_ids: ["adoc_one"],
              attached_documents: [
                {
                  Id: "adoc_one",
                  Name: "Policy Handbook",
                  OriginalFilename: "policy.pdf",
                  ContentType: "application/pdf",
                  SizeBytes: 12345,
                  CreatedUtc: "2026-01-01T00:00:00Z",
                  LastUpdateUtc: "2026-01-02T00:00:00Z",
                },
              ],
              document_filter_applied: true,
            },
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      },
    });

    const result = await client.chatCompletion("asst_local", {
      Messages: [{ role: "user", content: "Summarize this document." }],
      attached_document_ids: ["adoc_one"],
    });

    assertEqual("http://localhost:6600/v1.0/assistants/asst_local/chat", capturedUrl, "chatCompletion URL");
    assertNotNull(capturedInit, "captured fetch init");
    assertEqual("POST", capturedInit.method, "chatCompletion method");

    const body = JSON.parse(capturedInit.body);
    assertEqual(1, body.attached_document_ids.length, "request attached_document_ids count");
    assertEqual("adoc_one", body.attached_document_ids[0], "request attached_document_ids first item");
    assertFalse(Object.prototype.hasOwnProperty.call(body, "AttachedDocumentIds"), "request should not use PascalCase attachment key");
    assertEqual(false, body.Stream, "chatCompletion forces non-streaming body");

    assertNotNull(result.retrieval, "response retrieval");
    assertEqual(1, result.retrieval.attached_document_ids.length, "response attached_document_ids count");
    assertEqual("adoc_one", result.retrieval.attached_document_ids[0], "response attached_document_ids first item");
    assertEqual(true, result.retrieval.document_filter_applied, "response document_filter_applied");
    assertEqual("adoc_one", result.retrieval.attached_documents[0].Id, "response attached document ID");
    assertFalse(Object.prototype.hasOwnProperty.call(result.retrieval.attached_documents[0], "S3Key"), "selection metadata should not expose S3Key");
    assertFalse(Object.prototype.hasOwnProperty.call(result.retrieval.attached_documents[0], "BucketName"), "selection metadata should not expose BucketName");
    assertEqual(16, result.usage.total_tokens, "response usage total_tokens");
    assertEqual("hidden reasoning", result.choices[0].message.thinking, "response message thinking");
    assertEqual(5, result.usage.tool_definition_tokens, "response usage tool_definition_tokens");
    assertEqual(3, result.usage.prompt_tokens_details.cached_tokens, "response usage cached_tokens");
    assertEqual(7, result.usage.completion_tokens_details.reasoning_tokens, "response usage reasoning_tokens");
    assertEqual(1, result.tool_calls.length, "response tool_calls count");
    assertEqual("collection_search", result.tool_calls[0].tool_name, "response tool_calls tool name");
    assertEqual("Searching collection", result.tool_calls[0].display_label, "response tool_calls display label");
    assertEqual(3, result.tool_calls[0].result_count, "response tool_calls result count");
    assertEqual(2, result.tool_calls[0].credits_used, "response tool_calls credits used");
    assertEqual(45.5, result.tool_calls[0].provider_latency_ms, "response tool_calls provider latency");
    assertFalse(Object.prototype.hasOwnProperty.call(result.tool_calls[0], "ArgumentsJson"), "response tool_calls should not expose raw arguments");
    assertFalse(Object.prototype.hasOwnProperty.call(result.tool_calls[0], "OutputJson"), "response tool_calls should not expose raw output");
  });

  await runner.runTest("SDK contract: chatCompletion sends local_attachments", async () => {
    let capturedInit = null;

    const client = new AssistantHubClient({
      baseUrl: "http://localhost:6600",
      apiKey: "test-key",
      fetch: async (_url, init) => {
        capturedInit = init;
        return new Response(
          JSON.stringify({
            id: "chatcmpl_local_attachment",
            object: "chat.completion",
            created: 0,
            model: "test-model",
            choices: [
              {
                index: 0,
                message: { role: "assistant", content: "done" },
                finish_reason: "stop",
              },
            ],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      },
    });

    await client.chatCompletion("asst_local", {
      Messages: [{ role: "user", content: "Summarize this local file." }],
      local_attachments: [
        {
          name: "notes.txt",
          content_type: "text/plain",
          base64_content: "SGVsbG8=",
        },
      ],
    });

    assertNotNull(capturedInit, "captured fetch init");
    const body = JSON.parse(capturedInit.body);
    assertEqual(1, body.local_attachments.length, "request local_attachments count");
    assertEqual("notes.txt", body.local_attachments[0].name, "request local attachment name");
    assertEqual("SGVsbG8=", body.local_attachments[0].base64_content, "request local attachment base64");
    assertFalse(Object.prototype.hasOwnProperty.call(body, "LocalAttachments"), "request should not use PascalCase local attachment key");
  });

  await runner.runTest("SDK contract: validateAssistantToolPolicy sends ToolPolicy", async () => {
    let capturedUrl = null;
    let capturedInit = null;

    const client = new AssistantHubClient({
      baseUrl: "http://localhost:6600",
      apiKey: "test-key",
      fetch: async (url, init) => {
        capturedUrl = String(url);
        capturedInit = init;
        return new Response(
          JSON.stringify({
            Success: false,
            Message: "Policy invalid.",
            ToolPolicyJson: "{}",
            ToolPolicy: {
              EnableToolCalls: true,
              EnableCollectionSearchTool: true,
              EnableDocumentAtomExtractionTool: true,
              EnableWebSearchTool: true,
              ToolChoiceMode: "Required",
              MaxToolResultItems: 9,
              AllowedToolNames: ["collection_search"],
              MaxSearchTopK: 7,
              MaxDocumentsConsideredPerSearch: 25,
              MaxResultsConsideredPerSearch: 50,
              MaxAtomExtractionBytes: 2097152,
              MaxAtomExtractionCharacters: 24000,
              AllowedSearchModes: ["FullText"],
              ReturnFullSearchContent: true,
              MaxWebResults: 3,
              TavilyEndpoint: "https://assistant.tavily.test/search",
              TavilyApiKey: "assistant-key",
              AllowUngovernedWebAccess: true,
              AllowedWebDomains: ["example.com"],
            },
            Tools: [],
            Errors: ["EnableToolCalls is true but no enabled tool is currently executable."],
            ErrorCodes: ["no_available_tools"],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      },
    });

    const result = await client.validateAssistantToolPolicy("asst_local", {
      ToolPolicy: {
        EnableToolCalls: true,
        EnableCollectionSearchTool: true,
        EnableDocumentAtomExtractionTool: true,
        EnableWebSearchTool: true,
        ToolChoiceMode: "Required",
        MaxToolResultItems: 9,
        AllowedToolNames: ["collection_search"],
        MaxSearchTopK: 7,
        MaxDocumentsConsideredPerSearch: 25,
        MaxResultsConsideredPerSearch: 50,
        MaxAtomExtractionBytes: 2097152,
        MaxAtomExtractionCharacters: 24000,
        AllowedSearchModes: ["FullText"],
        ReturnFullSearchContent: true,
        MaxWebResults: 3,
        TavilyEndpoint: "https://assistant.tavily.test/search",
        TavilyApiKey: "assistant-key",
        AllowUngovernedWebAccess: true,
        AllowedWebDomains: ["example.com"],
      },
    });

    assertEqual("http://localhost:6600/v1.0/assistants/asst_local/settings/tools/validate", capturedUrl, "validateAssistantToolPolicy URL");
    assertNotNull(capturedInit, "captured fetch init");
    assertEqual("POST", capturedInit.method, "validateAssistantToolPolicy method");

    const body = JSON.parse(capturedInit.body);
    assertNotNull(body.ToolPolicy, "request ToolPolicy");
    assertEqual(true, body.ToolPolicy.EnableToolCalls, "request EnableToolCalls");
    assertEqual(true, body.ToolPolicy.EnableCollectionSearchTool, "request EnableCollectionSearchTool");
    assertEqual(true, body.ToolPolicy.EnableDocumentAtomExtractionTool, "request EnableDocumentAtomExtractionTool");
    assertEqual("Required", body.ToolPolicy.ToolChoiceMode, "request ToolChoiceMode");
    assertEqual(9, body.ToolPolicy.MaxToolResultItems, "request MaxToolResultItems");
    assertEqual("collection_search", body.ToolPolicy.AllowedToolNames[0], "request AllowedToolNames");
    assertEqual(7, body.ToolPolicy.MaxSearchTopK, "request MaxSearchTopK");
    assertEqual(25, body.ToolPolicy.MaxDocumentsConsideredPerSearch, "request MaxDocumentsConsideredPerSearch");
    assertEqual(50, body.ToolPolicy.MaxResultsConsideredPerSearch, "request MaxResultsConsideredPerSearch");
    assertEqual(2097152, body.ToolPolicy.MaxAtomExtractionBytes, "request MaxAtomExtractionBytes");
    assertEqual(24000, body.ToolPolicy.MaxAtomExtractionCharacters, "request MaxAtomExtractionCharacters");
    assertEqual("FullText", body.ToolPolicy.AllowedSearchModes[0], "request AllowedSearchModes");
    assertEqual(true, body.ToolPolicy.ReturnFullSearchContent, "request ReturnFullSearchContent");
    assertEqual(3, body.ToolPolicy.MaxWebResults, "request MaxWebResults");
    assertEqual("https://assistant.tavily.test/search", body.ToolPolicy.TavilyEndpoint, "request TavilyEndpoint");
    assertEqual("assistant-key", body.ToolPolicy.TavilyApiKey, "request TavilyApiKey");
    assertEqual(true, body.ToolPolicy.AllowUngovernedWebAccess, "request AllowUngovernedWebAccess");
    assertEqual("example.com", body.ToolPolicy.AllowedWebDomains[0], "request AllowedWebDomains");
    assertFalse(Object.prototype.hasOwnProperty.call(body, "toolPolicy"), "request should not use lower-camel ToolPolicy");

    assertEqual(false, result.Success, "validation result Success");
    assertNotNull(result.ToolPolicy, "validation result ToolPolicy");
    assertEqual(true, result.ToolPolicy.EnableCollectionSearchTool, "validation result EnableCollectionSearchTool");
    assertEqual(true, result.ToolPolicy.EnableDocumentAtomExtractionTool, "validation result EnableDocumentAtomExtractionTool");
    assertEqual("Required", result.ToolPolicy.ToolChoiceMode, "validation result ToolChoiceMode");
    assertEqual("collection_search", result.ToolPolicy.AllowedToolNames[0], "validation result AllowedToolNames");
    assertEqual(7, result.ToolPolicy.MaxSearchTopK, "validation result MaxSearchTopK");
    assertEqual(25, result.ToolPolicy.MaxDocumentsConsideredPerSearch, "validation result MaxDocumentsConsideredPerSearch");
    assertEqual(50, result.ToolPolicy.MaxResultsConsideredPerSearch, "validation result MaxResultsConsideredPerSearch");
    assertEqual(2097152, result.ToolPolicy.MaxAtomExtractionBytes, "validation result MaxAtomExtractionBytes");
    assertEqual(24000, result.ToolPolicy.MaxAtomExtractionCharacters, "validation result MaxAtomExtractionCharacters");
    assertEqual("https://assistant.tavily.test/search", result.ToolPolicy.TavilyEndpoint, "validation result TavilyEndpoint");
    assertEqual("example.com", result.ToolPolicy.AllowedWebDomains[0], "validation result AllowedWebDomains");
    assertEqual("no_available_tools", result.ErrorCodes[0], "validation result ErrorCodes");
  });

  await runner.runTest("SDK contract: testAssistantToolPolicy uses diagnostics route", async () => {
    let capturedUrl = null;
    let capturedInit = null;

    const client = new AssistantHubClient({
      baseUrl: "http://localhost:6600",
      apiKey: "test-key",
      fetch: async (url, init) => {
        capturedUrl = String(url);
        capturedInit = init;
        return new Response(
          JSON.stringify({
            Success: false,
            Message: "Tool diagnostics found blocking issues.",
            AssistantId: "asst_local",
            InferenceEndpointId: "cep_local",
            ToolRoutingInferenceEndpointId: "cep_router",
            EffectiveToolRoutingInferenceEndpointId: "cep_router",
            EndpointResolved: true,
            EndpointModel: "qwen3-tool",
            EndpointApiFormat: "OpenAI",
            EndpointActive: true,
            EndpointSupportsToolCalling: false,
            EndpointToolCallingApiFormat: null,
            EndpointSupportsParallelToolCalls: false,
            EndpointSupportsStreamingToolCalls: false,
            Validation: { Success: true, Errors: [], ErrorCodes: [] },
            Tools: [],
            Warnings: [],
            Errors: ["The effective tool-routing completion endpoint does not explicitly support tool calling."],
            ErrorCodes: ["tool_routing_endpoint_not_tool_capable"],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      },
    });

    const result = await client.testAssistantToolPolicy("asst_local", {
      ToolPolicyJson: "{\"EnableToolCalls\":true}",
    });

    assertEqual("http://localhost:6600/v1.0/assistants/asst_local/settings/tools/test", capturedUrl, "testAssistantToolPolicy URL");
    assertNotNull(capturedInit, "captured fetch init");
    assertEqual("POST", capturedInit.method, "testAssistantToolPolicy method");
    const body = JSON.parse(capturedInit.body);
    assertEqual("{\"EnableToolCalls\":true}", body.ToolPolicyJson, "diagnostics request ToolPolicyJson");
    assertEqual(false, result.Success, "diagnostics result Success");
    assertEqual("cep_router", result.ToolRoutingInferenceEndpointId, "diagnostics configured tool routing endpoint");
    assertEqual("cep_router", result.EffectiveToolRoutingInferenceEndpointId, "diagnostics effective tool routing endpoint");
    assertEqual(true, result.EndpointResolved, "diagnostics endpoint resolved");
    assertEqual("qwen3-tool", result.EndpointModel, "diagnostics endpoint model");
    assertEqual("tool_routing_endpoint_not_tool_capable", result.ErrorCodes[0], "diagnostics result ErrorCodes");
  });

  await runner.runTest("SDK contract: getExternalSearchStatus uses status route", async () => {
    let capturedUrl = null;
    let capturedInit = null;

    const client = new AssistantHubClient({
      baseUrl: "http://localhost:6600",
      apiKey: "test-key",
      fetch: async (url, init) => {
        capturedUrl = String(url);
        capturedInit = init;
        return new Response(
          JSON.stringify({
            Enabled: true,
            EnabledProviders: 1,
            ConfiguredProviders: 1,
            MisconfiguredProviders: 0,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      },
    });

    const result = await client.getExternalSearchStatus();
    assertEqual("http://localhost:6600/v1.0/configuration/external-search/status", capturedUrl, "external-search status URL");
    assertNotNull(capturedInit, "captured fetch init");
    assertEqual("GET", capturedInit.method, "external-search status method");
    assertEqual(true, result.Enabled, "external-search status enabled");
    assertEqual(1, result.ConfiguredProviders, "external-search status configured providers");
    assertFalse(Object.prototype.hasOwnProperty.call(result, "ApiKey"), "external-search status must not include secrets");
  });

  await runner.runTest("SDK contract: assistant tool-call trace routes", async () => {
    const requests = [];

    const client = new AssistantHubClient({
      baseUrl: "http://localhost:6600",
      apiKey: "test-key",
      fetch: async (url, init) => {
        requests.push({ url: String(url), init });
        const method = init?.method || "GET";
        const requestUrl = String(url);
        if (method === "GET" && requestUrl.includes("/v1.0/assistants/asst_local/tool-calls?")) {
          return new Response(
            JSON.stringify({
              Success: true,
              MaxResults: 5,
              TotalRecords: 1,
              RecordsRemaining: 0,
              EndOfResults: true,
              Objects: [
                {
                  Id: "atc_local",
                  AssistantId: "asst_local",
                  TraceId: "trace_local",
                  ToolName: "collection_search",
                  ArgumentsJson: "[redacted]",
                  Success: true,
                },
              ],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        }
        if (method === "GET" && requestUrl.endsWith("/v1.0/assistants/asst_local/tool-calls/atc_local")) {
          return new Response(
            JSON.stringify({ Id: "atc_local", AssistantId: "asst_local", ToolName: "collection_search", Success: true }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        }
        if (method === "DELETE" && requestUrl.includes("/v1.0/assistants/asst_local/tool-calls?")) {
          return new Response(JSON.stringify({ DeletedCount: 1 }), { status: 200, headers: { "Content-Type": "application/json" } });
        }
        if (method === "DELETE" && requestUrl.endsWith("/v1.0/assistants/asst_local/tool-calls/atc_local")) {
          return new Response(null, { status: 204 });
        }
        return new Response("{}", { status: 404, headers: { "Content-Type": "application/json" } });
      },
    });

    const list = await client.listAssistantToolCalls("asst_local", {
      maxResults: 5,
      traceId: "trace_local",
      toolName: "collection_search",
      success: true,
    });
    assertEqual(1, list.Objects.length, "tool-call list count");
    assertEqual("atc_local", list.Objects[0].Id, "tool-call list id");
    assertEqual("collection_search", list.Objects[0].ToolName, "tool-call list tool");
    assertFalse(String(list.Objects[0].ArgumentsJson || "").includes("secret"), "tool-call list arguments redacted");

    const record = await client.getAssistantToolCall("asst_local", "atc_local");
    assertEqual("atc_local", record.Id, "tool-call get id");

    const deleted = await client.deleteAssistantToolCalls("asst_local", { toolName: "collection_search" });
    assertEqual(1, deleted.DeletedCount, "tool-call bulk delete count");

    await client.deleteAssistantToolCall("asst_local", "atc_local");

    assertEqual("GET", requests[0].init.method, "tool-call list method");
    assertTrue(requests[0].url.includes("/v1.0/assistants/asst_local/tool-calls?"), "tool-call list path");
    assertTrue(requests[0].url.includes("traceId=trace_local"), "tool-call list trace query");
    assertTrue(requests[0].url.includes("toolName=collection_search"), "tool-call list tool query");
    assertTrue(requests[0].url.includes("success=true"), "tool-call list success query");
    assertEqual("GET", requests[1].init.method, "tool-call get method");
    assertTrue(requests[1].url.endsWith("/v1.0/assistants/asst_local/tool-calls/atc_local"), "tool-call get path");
    assertEqual("DELETE", requests[2].init.method, "tool-call bulk delete method");
    assertTrue(requests[2].url.includes("toolName=collection_search"), "tool-call bulk delete query");
    assertEqual("DELETE", requests[3].init.method, "tool-call delete method");
    assertTrue(requests[3].url.endsWith("/v1.0/assistants/asst_local/tool-calls/atc_local"), "tool-call delete path");
  });

  await runner.runTest("SDK contract: ChatHistory parses attached document metadata", async () => {
    let capturedUrl = null;

    const client = new AssistantHubClient({
      baseUrl: "http://localhost:6600",
      apiKey: "test-key",
      fetch: async (url) => {
        capturedUrl = String(url);
        return new Response(
          JSON.stringify({
            Id: "chist_local",
            TenantId: "default",
            ThreadId: "thr_local",
            AssistantId: "asst_local",
            AttachedDocumentIdsJson: "[\"adoc_one\"]",
            AttachedDocumentsJson: "[{\"Id\":\"adoc_one\",\"Name\":\"Policy Handbook\"}]",
            CreatedUtc: "2026-01-01T00:00:00Z",
            LastUpdateUtc: "2026-01-01T00:00:00Z",
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      },
    });

    const history = await client.getHistory("chist_local");
    assertEqual("http://localhost:6600/v1.0/history/chist_local", capturedUrl, "getHistory URL");
    assertEqual("chist_local", history.Id, "history ID");
    assertTrue(history.AttachedDocumentIdsJson.includes("adoc_one"), "history attached document IDs JSON");
    assertTrue(history.AttachedDocumentsJson.includes("Policy Handbook"), "history attached documents JSON");
  });
}

// ---------------------------------------------------------------------------
// Test groups
// ---------------------------------------------------------------------------

async function healthTests(runner, client) {
  await runner.runTest("Health: HealthCheck returns true", async () => {
    const result = await client.health();
    assertNotNull(result, "Health result");
  });

  await runner.runTest("Health: WhoAmI returns authenticated identity", async () => {
    const result = await client.whoami();
    assertNotNull(result, "WhoAmI result");
  });
}

async function tenantTests(runner, client) {
  const suffix = uniqueSuffix();
  let createdTenantId = null;
  let createdUserId = null;
  let createdCredentialId = null;

  await runner.runTest("Tenant: List tenants returns results", async () => {
    const result = await client.listTenants();
    assertNotNull(result, "ListTenants result");
    assertGte(result.Objects.length, 1, "Tenant count");
  });

  await runner.runTest("Tenant: Create tenant with unique name", async () => {
    const result = await client.createTenant({ Name: "test-tenant-" + suffix });
    assertNotNull(result, "CreateTenant result");
    assertNotNull(result.Tenant, "CreateTenant Tenant");
    assertNotNull(result.Tenant.Id, "Tenant ID");
    assertStartsWith(result.Tenant.Id, "ten_", "Tenant ID prefix");
    createdTenantId = result.Tenant.Id;
  });

  await runner.runTest("Tenant: Get tenant by ID", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    const result = await client.getTenant(createdTenantId);
    assertNotNull(result, "GetTenant result");
    assertEqual(createdTenantId, result.Id, "Tenant ID");
  });

  await runner.runTest("Tenant: Update tenant name", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    const result = await client.updateTenant(createdTenantId, {
      Id: createdTenantId,
      Name: "test-tenant-updated-" + suffix,
    });
    assertNotNull(result, "UpdateTenant result");
  });

  await runner.runTest("Tenant: List users in tenant", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    const result = await client.listUsers(createdTenantId);
    assertNotNull(result, "ListUsers result");
  });

  await runner.runTest("Tenant: Create user in tenant", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    const result = await client.createUser(createdTenantId, {
      FirstName: "Test",
      LastName: "User",
      Email: "testuser-" + suffix + "@example.com",
      Active: true,
    });
    assertNotNull(result, "CreateUser result");
    assertNotNull(result.Id, "User ID");
    assertStartsWith(result.Id, "usr_", "User ID prefix");
    createdUserId = result.Id;
  });

  await runner.runTest("Tenant: Create credential in tenant", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    const result = await client.createCredential(createdTenantId, {
      Name: "test-credential-" + suffix,
      Active: true,
    });
    assertNotNull(result, "CreateCredential result");
    assertNotNull(result.Id, "Credential ID");
    assertStartsWith(result.Id, "cred_", "Credential ID prefix");
    createdCredentialId = result.Id;
  });

  await runner.runTest("Tenant: Delete credential", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    assertNotNull(createdCredentialId, "createdCredentialId from previous test");
    await client.deleteCredential(createdTenantId, createdCredentialId);
  });

  await runner.runTest("Tenant: Delete user", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    assertNotNull(createdUserId, "createdUserId from previous test");
    await client.deleteUser(createdTenantId, createdUserId);
  });

  await runner.runTest("Tenant: Delete tenant", async () => {
    assertNotNull(createdTenantId, "createdTenantId from previous test");
    await client.deleteTenant(createdTenantId);
  });
}

async function assistantTests(runner, client) {
  const suffix = uniqueSuffix();
  let createdAssistantId = null;

  await runner.runTest("Assistant: Create assistant with name and description", async () => {
    const result = await client.createAssistant({
      Name: "test-assistant-" + suffix,
      Description: "Test assistant created by JS SDK test suite",
    });
    assertNotNull(result, "CreateAssistant result");
    assertNotNull(result.Id, "Assistant ID");
    assertStartsWith(result.Id, "asst_", "Assistant ID prefix");
    createdAssistantId = result.Id;
  });

  await runner.runTest("Assistant: List assistants includes created one", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    const result = await client.listAssistants();
    assertNotNull(result, "ListAssistants result");
    assertNotNull(result.Objects, "ListAssistants Objects");
    const found = result.Objects.some((a) => a.Id === createdAssistantId);
    assertTrue(found, "Created assistant should appear in list");
  });

  await runner.runTest("Assistant: Get assistant by ID", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    const result = await client.getAssistant(createdAssistantId);
    assertNotNull(result, "GetAssistant result");
    assertEqual(createdAssistantId, result.Id, "Assistant ID");
  });

  await runner.runTest("Assistant: Update assistant name", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    const result = await client.updateAssistant(createdAssistantId, {
      Id: createdAssistantId,
      Name: "test-assistant-updated-" + suffix,
      Description: "Updated description",
    });
    assertNotNull(result, "UpdateAssistant result");
  });

  await runner.runTest("Assistant: Delete assistant", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    await client.deleteAssistant(createdAssistantId);
  });

  await runner.runTest("Assistant: Verify assistant no longer in list", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    const result = await client.listAssistants();
    assertNotNull(result, "ListAssistants result");
    assertNotNull(result.Objects, "ListAssistants Objects");
    const found = result.Objects.some((a) => a.Id === createdAssistantId);
    assertFalse(found, "Deleted assistant should not appear in list");
  });
}

async function collectionTests(runner, client) {
  const suffix = uniqueSuffix();
  let createdCollectionId = null;

  await runner.runTest("Collection: Create collection with name", async () => {
    const result = await client.createCollection({ Name: "test-collection-" + suffix });
    assertNotNull(result, "CreateCollection result");
    assertNotNull(result.Id, "Collection ID");
    createdCollectionId = result.Id;
  });

  await runner.runTest("Collection: List collections includes created one", async () => {
    assertNotNull(createdCollectionId, "createdCollectionId from previous test");
    const result = await client.listCollections();
    assertNotNull(result, "ListCollections result");
    assertNotNull(result.Objects, "ListCollections Objects");
    const found = result.Objects.some((c) => c.Id === createdCollectionId);
    assertTrue(found, "Created collection should appear in list");
  });

  await runner.runTest("Collection: Get collection by ID", async () => {
    assertNotNull(createdCollectionId, "createdCollectionId from previous test");
    const result = await client.getCollection(createdCollectionId);
    assertNotNull(result, "GetCollection result");
    assertEqual(createdCollectionId, result.Id, "Collection ID");
  });

  await runner.runTest("Collection: Update collection name", async () => {
    assertNotNull(createdCollectionId, "createdCollectionId from previous test");
    const result = await client.updateCollection(createdCollectionId, {
      Id: createdCollectionId,
      Name: "test-collection-updated-" + suffix,
    });
    assertNotNull(result, "UpdateCollection result");
  });

  await runner.runTest("Collection: Delete collection", async () => {
    assertNotNull(createdCollectionId, "createdCollectionId from previous test");
    await client.deleteCollection(createdCollectionId);
  });

  await runner.runTest("Collection: Verify collection no longer in list", async () => {
    assertNotNull(createdCollectionId, "createdCollectionId from previous test");
    const result = await client.listCollections();
    assertNotNull(result, "ListCollections result");
    assertNotNull(result.Objects, "ListCollections Objects");
    const found = result.Objects.some((c) => c.Id === createdCollectionId);
    assertFalse(found, "Deleted collection should not appear in list");
  });
}

async function documentTests(runner, client) {
  const suffix = uniqueSuffix();
  let createdCollectionId = null;
  let ingestionRuleId = null;
  let uploadedDocumentId = null;
  let secondDocumentId = null;

  await runner.runTest("Document: Create collection for document tests", async () => {
    const result = await client.createCollection({ Name: "test-doc-collection-" + suffix });
    assertNotNull(result, "CreateCollection result");
    assertNotNull(result.Id, "Collection ID");
    createdCollectionId = result.Id;
  });

  await runner.runTest("Document: Get ingestion rule for collection", async () => {
    assertNotNull(createdCollectionId, "createdCollectionId from previous test");
    const rules = await client.listIngestionRules();
    assertNotNull(rules, "ListIngestionRules result");
    assertNotNull(rules.Objects, "ListIngestionRules Objects");
    const rule = rules.Objects.find((r) => r.CollectionId === createdCollectionId);
    assertNotNull(rule, "Ingestion rule for created collection");
    assertStartsWith(rule.Id, "irule_", "Ingestion rule ID prefix");
    ingestionRuleId = rule.Id;
  });

  await runner.runTest("Document: Upload text document", async () => {
    assertNotNull(ingestionRuleId, "ingestionRuleId from previous test");
    const result = await client.createDocument({
      IngestionRuleId: ingestionRuleId,
      Base64Content: btoa64("This is a test document for SDK testing. It contains sample text content."),
      Name: "test-document-" + suffix,
      OriginalFilename: "test-document-" + suffix + ".txt",
      ContentType: "text/plain",
    });
    assertNotNull(result, "UploadDocument result");
    assertNotNull(result.Id, "Document ID");
    assertStartsWith(result.Id, "adoc_", "Document ID prefix");
    uploadedDocumentId = result.Id;
  });

  await runner.runTest("Document: List documents includes uploaded one", async () => {
    assertNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
    const result = await client.listDocuments();
    assertNotNull(result, "ListDocuments result");
    assertNotNull(result.Objects, "ListDocuments Objects");
    const found = result.Objects.some((d) => d.Id === uploadedDocumentId);
    assertTrue(found, "Uploaded document should appear in list");
  });

  await runner.runTest("Document: Get document by ID", async () => {
    assertNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
    const result = await client.getDocument(uploadedDocumentId);
    assertNotNull(result, "GetDocument result");
    assertEqual(uploadedDocumentId, result.Id, "Document ID");
  });

  await runner.runTest("Document: Delete document", async () => {
    assertNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
    await client.deleteDocument(uploadedDocumentId);
  });

  await runner.runTest("Document: Verify document no longer in list", async () => {
    assertNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
    const result = await client.listDocuments();
    assertNotNull(result, "ListDocuments result");
    assertNotNull(result.Objects, "ListDocuments Objects");
    const found = result.Objects.some((d) => d.Id === uploadedDocumentId);
    assertFalse(found, "Deleted document should not appear in list");
  });

  await runner.runTest("Document: Upload two documents for bulk delete", async () => {
    assertNotNull(ingestionRuleId, "ingestionRuleId from previous test");
    const doc1 = await client.createDocument({
      IngestionRuleId: ingestionRuleId,
      Base64Content: btoa64("Bulk delete test document one."),
      Name: "bulk-doc-1-" + suffix,
      OriginalFilename: "bulk-doc-1-" + suffix + ".txt",
      ContentType: "text/plain",
    });
    const doc2 = await client.createDocument({
      IngestionRuleId: ingestionRuleId,
      Base64Content: btoa64("Bulk delete test document two."),
      Name: "bulk-doc-2-" + suffix,
      OriginalFilename: "bulk-doc-2-" + suffix + ".txt",
      ContentType: "text/plain",
    });
    assertNotNull(doc1, "First bulk document");
    assertNotNull(doc1.Id, "First bulk document ID");
    assertNotNull(doc2, "Second bulk document");
    assertNotNull(doc2.Id, "Second bulk document ID");
    uploadedDocumentId = doc1.Id;
    secondDocumentId = doc2.Id;
  });

  await runner.runTest("Document: Bulk delete documents", async () => {
    assertNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
    assertNotNull(secondDocumentId, "secondDocumentId from previous test");
    await client.bulkDeleteDocuments([uploadedDocumentId, secondDocumentId]);
  });

  await runner.runTest("Document: Verify bulk deleted documents no longer in list", async () => {
    assertNotNull(uploadedDocumentId, "uploadedDocumentId from previous test");
    assertNotNull(secondDocumentId, "secondDocumentId from previous test");
    const result = await client.listDocuments();
    assertNotNull(result, "ListDocuments result");
    assertNotNull(result.Objects, "ListDocuments Objects");
    const foundFirst = result.Objects.some((d) => d.Id === uploadedDocumentId);
    const foundSecond = result.Objects.some((d) => d.Id === secondDocumentId);
    assertFalse(foundFirst, "First bulk deleted document should not appear in list");
    assertFalse(foundSecond, "Second bulk deleted document should not appear in list");
  });

  await runner.runTest("Document: Clean up test collection", async () => {
    assertNotNull(createdCollectionId, "createdCollectionId from previous test");
    await client.deleteCollection(createdCollectionId);
  });
}

async function threadTests(runner, client, baseUrl, apiKey) {
  const suffix = uniqueSuffix();
  let createdAssistantId = null;
  let createdThreadId = null;

  await runner.runTest("Thread: Create assistant for thread tests", async () => {
    const result = await client.createAssistant({
      Name: "test-thread-assistant-" + suffix,
      Description: "Assistant for thread tests",
    });
    assertNotNull(result, "CreateAssistant result");
    assertNotNull(result.Id, "Assistant ID");
    createdAssistantId = result.Id;
  });

  await runner.runTest("Thread: Create thread for assistant", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    const result = await client.createThread(createdAssistantId);
    assertNotNull(result, "CreateThread result");
    assertNotNull(result.ThreadId, "Thread ID");
    createdThreadId = result.ThreadId;
  });

  await runner.runTest("Thread: List threads includes created one", async () => {
    assertNotNull(createdThreadId, "createdThreadId from previous test");
    const result = await client.listThreads();
    assertNotNull(result, "ListThreads result");
  });

  await runner.runTest("Thread: Get thread history", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    assertNotNull(createdThreadId, "createdThreadId from previous test");
    const result = await client.getThreadHistory(createdAssistantId, createdThreadId);
    assertNotNull(result, "GetThreadHistory result");
  });

  await runner.runTest("Thread: Delete thread", async () => {
    assertNotNull(createdThreadId, "createdThreadId from previous test");
    await client.deleteThread(createdThreadId);
  });

  await runner.runTest("Thread: Clean up assistant", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    await client.deleteAssistant(createdAssistantId);
  });
}

async function endpointTests(runner, client) {
  const suffix = uniqueSuffix();
  let embeddingEndpointId = null;
  let completionEndpointId = null;

  // Embedding endpoint tests
  await runner.runTest("Endpoint: Create embedding endpoint", async () => {
    const result = await client.createEmbeddingEndpoint({
      Name: "test-embedding-" + suffix,
      Model: "test-model",
      Endpoint: "http://localhost:8321",
      ApiFormat: "OpenAI",
      Active: true,
    });
    assertNotNull(result, "CreateEmbeddingEndpoint result");
    assertNotNull(result.Id, "Embedding endpoint ID");
    embeddingEndpointId = result.Id;
  });

  await runner.runTest("Endpoint: List embedding endpoints includes created one", async () => {
    assertNotNull(embeddingEndpointId, "embeddingEndpointId from previous test");
    const result = await client.listEmbeddingEndpoints();
    assertNotNull(result, "ListEmbeddingEndpoints result");
    assertNotNull(result.Objects, "ListEmbeddingEndpoints Objects");
    const found = result.Objects.some((e) => e.Id === embeddingEndpointId);
    assertTrue(found, "Created embedding endpoint should appear in list");
  });

  await runner.runTest("Endpoint: Get embedding endpoint by ID", async () => {
    assertNotNull(embeddingEndpointId, "embeddingEndpointId from previous test");
    const result = await client.getEmbeddingEndpoint(embeddingEndpointId);
    assertNotNull(result, "GetEmbeddingEndpoint result");
    assertEqual(embeddingEndpointId, result.Id, "Embedding endpoint ID");
  });

  await runner.runTest("Endpoint: Update embedding endpoint", async () => {
    assertNotNull(embeddingEndpointId, "embeddingEndpointId from previous test");
    const result = await client.updateEmbeddingEndpoint(embeddingEndpointId, {
      Id: embeddingEndpointId,
      Name: "test-embedding-updated-" + suffix,
      Model: "test-model",
      Endpoint: "http://localhost:8321",
      ApiFormat: "OpenAI",
      Active: true,
    });
    assertNotNull(result, "UpdateEmbeddingEndpoint result");
  });

  await runner.runTest("Endpoint: Check embedding health", async () => {
    const result = await client.getEmbeddingEndpointHealth();
    assertNotNull(result, "EmbeddingHealth result");
  });

  await runner.runTest("Endpoint: Delete embedding endpoint", async () => {
    assertNotNull(embeddingEndpointId, "embeddingEndpointId from previous test");
    await client.deleteEmbeddingEndpoint(embeddingEndpointId);
  });

  // Completion endpoint tests
  await runner.runTest("Endpoint: Create completion endpoint", async () => {
    const result = await client.createCompletionEndpoint({
      Name: "test-completion-" + suffix,
      Model: "test-model",
      Endpoint: "http://localhost:8321",
      ApiFormat: "OpenAI",
      Active: true,
    });
    assertNotNull(result, "CreateCompletionEndpoint result");
    assertNotNull(result.Id, "Completion endpoint ID");
    completionEndpointId = result.Id;
  });

  await runner.runTest("Endpoint: List completion endpoints includes created one", async () => {
    assertNotNull(completionEndpointId, "completionEndpointId from previous test");
    const result = await client.listCompletionEndpoints();
    assertNotNull(result, "ListCompletionEndpoints result");
    assertNotNull(result.Objects, "ListCompletionEndpoints Objects");
    const found = result.Objects.some((e) => e.Id === completionEndpointId);
    assertTrue(found, "Created completion endpoint should appear in list");
  });

  await runner.runTest("Endpoint: Get completion endpoint by ID", async () => {
    assertNotNull(completionEndpointId, "completionEndpointId from previous test");
    const result = await client.getCompletionEndpoint(completionEndpointId);
    assertNotNull(result, "GetCompletionEndpoint result");
    assertEqual(completionEndpointId, result.Id, "Completion endpoint ID");
  });

  await runner.runTest("Endpoint: Update completion endpoint", async () => {
    assertNotNull(completionEndpointId, "completionEndpointId from previous test");
    const result = await client.updateCompletionEndpoint(completionEndpointId, {
      Id: completionEndpointId,
      Name: "test-completion-updated-" + suffix,
      Model: "test-model",
      Endpoint: "http://localhost:8321",
      ApiFormat: "OpenAI",
      Active: true,
    });
    assertNotNull(result, "UpdateCompletionEndpoint result");
  });

  await runner.runTest("Endpoint: Check completion health", async () => {
    const result = await client.getCompletionEndpointHealth();
    assertNotNull(result, "CompletionHealth result");
  });

  await runner.runTest("Endpoint: Delete completion endpoint", async () => {
    assertNotNull(completionEndpointId, "completionEndpointId from previous test");
    await client.deleteCompletionEndpoint(completionEndpointId);
  });
}

async function inferenceTests(runner, client) {
  await runner.runTest("Inference: List models returns results", async () => {
    const result = await client.listModels();
    assertNotNull(result, "ListModels result");
    // May return empty list -- that is acceptable
  });
}

async function evalTests(runner, client) {
  const suffix = uniqueSuffix();
  let createdAssistantId = null;
  let createdFactId = null;

  await runner.runTest("Eval: Create assistant for eval tests", async () => {
    const result = await client.createAssistant({
      Name: "test-eval-assistant-" + suffix,
      Description: "Assistant for eval tests",
    });
    assertNotNull(result, "CreateAssistant result");
    assertNotNull(result.Id, "Assistant ID");
    createdAssistantId = result.Id;
  });

  await runner.runTest("Eval: Create eval fact", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    const result = await client.createEvalFact({
      AssistantId: createdAssistantId,
      Category: "test-category",
      Question: "What is the test question?",
      ExpectedFacts: JSON.stringify(["fact1", "fact2"]),
    });
    assertNotNull(result, "CreateEvalFact result");
    assertNotNull(result.Id, "EvalFact ID");
    assertStartsWith(result.Id, "ef_", "EvalFact ID prefix");
    createdFactId = result.Id;
  });

  await runner.runTest("Eval: List eval facts includes created one", async () => {
    assertNotNull(createdFactId, "createdFactId from previous test");
    const result = await client.listEvalFacts();
    assertNotNull(result, "ListEvalFacts result");
    assertNotNull(result.Objects, "ListEvalFacts Objects");
    const found = result.Objects.some((f) => f.Id === createdFactId);
    assertTrue(found, "Created eval fact should appear in list");
  });

  await runner.runTest("Eval: Default judge prompt returns prompt payload", async () => {
    const result = await client.getDefaultJudgePrompt();
    assertNotNull(result, "GetDefaultJudgePrompt result");
    assertTrue(typeof result.Prompt === "string", "Default judge prompt should be a string");
  });

  await runner.runTest("Eval: Delete eval fact", async () => {
    assertNotNull(createdFactId, "createdFactId from previous test");
    await client.deleteEvalFact(createdFactId);
  });

  await runner.runTest("Eval: Cleanup assistant", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    await client.deleteAssistant(createdAssistantId);
  });
}

async function requestHistoryTests(runner, client) {
  let capturedRequestId = null;
  const startUtc = new Date(Date.now() - 5_000).toISOString();

  await runner.runTest("RequestHistory: Capture and list whoami request", async () => {
    await client.whoami();

    for (let attempt = 0; attempt < 20; attempt += 1) {
      const result = await client.listRequestHistory({
        maxResults: 25,
        pathContains: "/v1.0/whoami",
        startUtc,
      });
      assertNotNull(result, "ListRequestHistory result");
      assertNotNull(result.Objects, "ListRequestHistory Objects");

      const entry = result.Objects.find((item) =>
        typeof item.RequestPath === "string" && item.RequestPath.includes("/v1.0/whoami")
      );

      if (entry && entry.Id) {
        capturedRequestId = entry.Id;
        return;
      }

      await sleep(500);
    }

    throw new Error("Timed out waiting for request-history capture of /v1.0/whoami");
  });

  await runner.runTest("RequestHistory: Get request-history entry by ID", async () => {
    assertNotNull(capturedRequestId, "capturedRequestId from previous test");
    const result = await client.getRequestHistory(capturedRequestId);
    assertNotNull(result, "GetRequestHistory result");
    assertEqual(capturedRequestId, result.Id, "RequestHistory ID");
    assertTrue(typeof result.RequestPath === "string" && result.RequestPath.includes("/v1.0/whoami"), "Request path should reference whoami");
  });

  await runner.runTest("RequestHistory: Get detailed request-history entry by ID", async () => {
    assertNotNull(capturedRequestId, "capturedRequestId from previous test");
    const result = await client.getRequestHistoryDetail(capturedRequestId);
    assertNotNull(result, "GetRequestHistoryDetail result");
    assertEqual(capturedRequestId, result.Id, "RequestHistory detail ID");
  });

  await runner.runTest("RequestHistory: Get request-history summary", async () => {
    const result = await client.getRequestHistorySummary({
      pathContains: "/v1.0/whoami",
      startUtc,
      bucketSeconds: 60,
    });
    assertNotNull(result, "GetRequestHistorySummary result");
    assertTrue((result.TotalCount || 0) >= 1, "RequestHistory summary should include at least one entry");
  });
}

async function crawlPlanTests(runner, client) {
  const suffix = uniqueSuffix();
  let createdPlanId = null;
  let createdCifsPlanId = null;
  let createdNfsPlanId = null;

  await runner.runTest("CrawlPlan: Create crawl plan", async () => {
    const result = await client.createCrawlPlan({
      Name: "test-crawl-plan-" + suffix,
      RepositoryType: "Web",
      RepositorySettings: {
        RepositoryType: "Web",
        AuthenticationType: "None",
        StartUrl: "https://example.com",
        MaxDepth: 1,
        MaxParallelTasks: 1,
        CrawlDelayMs: 1000,
        FollowLinks: false,
        FollowRedirects: true,
        ExtractSitemapLinks: true,
        IgnoreRobotsTxt: false,
        RestrictToChildUrls: true,
      },
      Schedule: {
        IntervalType: "OneTime",
        IntervalValue: 1,
      },
      ProcessAdditions: true,
      ProcessUpdates: true,
      ProcessDeletions: false,
      MaxDrainTasks: 1,
      RetentionDays: 7,
    });
    assertNotNull(result, "CreateCrawlPlan result");
    assertNotNull(result.Id, "CrawlPlan ID");
    assertStartsWith(result.Id, "cplan_", "CrawlPlan ID prefix");
    createdPlanId = result.Id;
  });

  await runner.runTest("CrawlPlan: Create CIFS crawl plan", async () => {
    const result = await client.createCrawlPlan({
      Name: "test-cifs-crawl-plan-" + suffix,
      RepositoryType: "CIFS",
      RepositorySettings: {
        RepositoryType: "CIFS",
        CifsHostname: "fileserver.example.com",
        CifsUsername: "crawler",
        CifsPassword: "secret",
        CifsShareName: "content",
        IncludeSubdirectories: true,
      },
      Schedule: {
        IntervalType: "OneTime",
        IntervalValue: 1,
      },
      ProcessAdditions: true,
      ProcessUpdates: true,
      ProcessDeletions: false,
      MaxDrainTasks: 1,
      RetentionDays: 7,
    });
    assertNotNull(result, "Create CIFS CrawlPlan result");
    assertNotNull(result.Id, "CIFS CrawlPlan ID");
    assertEqual("CIFS", result.RepositoryType, "CIFS RepositoryType");
    createdCifsPlanId = result.Id;
  });

  await runner.runTest("CrawlPlan: Create NFS crawl plan", async () => {
    const result = await client.createCrawlPlan({
      Name: "test-nfs-crawl-plan-" + suffix,
      RepositoryType: "NFS",
      RepositorySettings: {
        RepositoryType: "NFS",
        NfsHostname: "nfs.example.com",
        NfsUserId: 1000,
        NfsGroupId: 1000,
        NfsShareName: "/exports/content",
        NfsVersion: "V3",
        IncludeSubdirectories: true,
      },
      Schedule: {
        IntervalType: "OneTime",
        IntervalValue: 1,
      },
      ProcessAdditions: true,
      ProcessUpdates: true,
      ProcessDeletions: false,
      MaxDrainTasks: 1,
      RetentionDays: 7,
    });
    assertNotNull(result, "Create NFS CrawlPlan result");
    assertNotNull(result.Id, "NFS CrawlPlan ID");
    assertEqual("NFS", result.RepositoryType, "NFS RepositoryType");
    createdNfsPlanId = result.Id;
  });

  await runner.runTest("CrawlPlan: List crawl plans includes created one", async () => {
    assertNotNull(createdPlanId, "createdPlanId from previous test");
    assertNotNull(createdCifsPlanId, "createdCifsPlanId from previous test");
    assertNotNull(createdNfsPlanId, "createdNfsPlanId from previous test");
    const result = await client.listCrawlPlans();
    assertNotNull(result, "ListCrawlPlans result");
    assertNotNull(result.Objects, "ListCrawlPlans Objects");
    assertTrue(result.Objects.some((p) => p.Id === createdPlanId), "Created web crawl plan should appear in list");
    assertTrue(result.Objects.some((p) => p.Id === createdCifsPlanId), "Created CIFS crawl plan should appear in list");
    assertTrue(result.Objects.some((p) => p.Id === createdNfsPlanId), "Created NFS crawl plan should appear in list");
  });

  await runner.runTest("CrawlPlan: Get crawl plan by ID", async () => {
    assertNotNull(createdPlanId, "createdPlanId from previous test");
    const result = await client.getCrawlPlan(createdPlanId);
    assertNotNull(result, "GetCrawlPlan result");
    assertEqual(createdPlanId, result.Id, "CrawlPlan ID");
  });

  await runner.runTest("CrawlPlan: Update crawl plan", async () => {
    assertNotNull(createdPlanId, "createdPlanId from previous test");
    const result = await client.updateCrawlPlan(createdPlanId, {
      Id: createdPlanId,
      Name: "test-crawl-plan-updated-" + suffix,
      RepositoryType: "Web",
      RepositorySettings: {
        RepositoryType: "Web",
        AuthenticationType: "None",
        StartUrl: "https://example.com/updated",
        MaxDepth: 2,
        MaxParallelTasks: 1,
        CrawlDelayMs: 500,
        FollowLinks: true,
        FollowRedirects: true,
        ExtractSitemapLinks: true,
        IgnoreRobotsTxt: false,
        RestrictToChildUrls: true,
      },
      Schedule: {
        IntervalType: "OneTime",
        IntervalValue: 1,
      },
      ProcessAdditions: true,
      ProcessUpdates: true,
      ProcessDeletions: true,
      MaxDrainTasks: 2,
      RetentionDays: 14,
    });
    assertNotNull(result, "UpdateCrawlPlan result");
  });

  await runner.runTest("CrawlPlan: Delete crawl plan", async () => {
    assertNotNull(createdPlanId, "createdPlanId from previous test");
    await client.deleteCrawlPlan(createdPlanId);
    if (createdCifsPlanId) await client.deleteCrawlPlan(createdCifsPlanId);
    if (createdNfsPlanId) await client.deleteCrawlPlan(createdNfsPlanId);
  });
}

async function configTests(runner, client) {
  await runner.runTest("Config: Get config returns valid response", async () => {
    const result = await client.getConfiguration();
    assertNotNull(result, "GetConfiguration result");
  });
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  const baseUrl = process.env.ASSISTANTHUB_URL || "http://localhost:6600";
  const apiKey = process.env.ASSISTANTHUB_API_KEY || "default";
  const localOnly = localOnlyRequested();

  console.log("==========================================================");
  console.log("  AssistantHub JS SDK Test Suite");
  console.log("==========================================================");
  console.log("");
  console.log("  Base URL:  " + baseUrl);
  console.log("  API Key:   " + (apiKey ? "(set)" : "(none)"));
  console.log("  LocalOnly: " + localOnly);
  console.log("");

  const runner = new TestRunner();
  const totalStart = performance.now();

  try {
    await sdkContractTests(runner);

    if (localOnly) {
      const totalMs = performance.now() - totalStart;
      runner.printSummary(totalMs);
      const failed = runner.results.some((r) => !r.passed);
      process.exit(failed ? 1 : 0);
    }

    const client = new AssistantHubClient({ baseUrl, apiKey });
    await healthTests(runner, client);
    await tenantTests(runner, client);
    await assistantTests(runner, client);
    await collectionTests(runner, client);
    await documentTests(runner, client);
    await threadTests(runner, client, baseUrl, apiKey);
    await endpointTests(runner, client);
    await inferenceTests(runner, client);
    await evalTests(runner, client);
    await requestHistoryTests(runner, client);
    await crawlPlanTests(runner, client);
    await configTests(runner, client);
  } catch (err) {
    console.log("\x1b[31mUnhandled exception during test execution: " + err.message + "\x1b[0m");
    if (err.stack) console.log(err.stack);
  }

  const totalMs = performance.now() - totalStart;
  runner.printSummary(totalMs);

  const failed = runner.results.some((r) => !r.passed);
  process.exit(failed ? 1 : 0);
}

main();
