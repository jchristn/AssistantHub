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
    // JS SDK does not expose deleteThread -- call the API directly
    const headers = { "Content-Type": "application/json" };
    if (apiKey) headers["Authorization"] = "Bearer " + apiKey;
    const resp = await fetch(baseUrl + "/v1.0/threads/" + encodeURIComponent(createdThreadId), {
      method: "DELETE",
      headers,
    });
    if (!resp.ok && resp.status !== 204) {
      throw new Error("Delete thread failed with status " + resp.status);
    }
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

  await runner.runTest("Eval: Delete eval fact", async () => {
    assertNotNull(createdFactId, "createdFactId from previous test");
    await client.deleteEvalFact(createdFactId);
  });

  await runner.runTest("Eval: Cleanup assistant", async () => {
    assertNotNull(createdAssistantId, "createdAssistantId from previous test");
    await client.deleteAssistant(createdAssistantId);
  });
}

async function crawlPlanTests(runner, client) {
  const suffix = uniqueSuffix();
  let createdPlanId = null;

  await runner.runTest("CrawlPlan: Create crawl plan", async () => {
    const result = await client.createCrawlPlan({
      Name: "test-crawl-plan-" + suffix,
      RepositoryType: "Web",
      RepositorySettings: {
        AuthType: "None",
        StartUrl: "https://example.com",
        MaxDepth: 1,
        MaxParallelTasks: 1,
        CrawlDelayMs: 1000,
        FollowLinks: false,
        FollowRedirects: true,
        RespectRobotsTxt: true,
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

  await runner.runTest("CrawlPlan: List crawl plans includes created one", async () => {
    assertNotNull(createdPlanId, "createdPlanId from previous test");
    const result = await client.listCrawlPlans();
    assertNotNull(result, "ListCrawlPlans result");
    assertNotNull(result.Objects, "ListCrawlPlans Objects");
    const found = result.Objects.some((p) => p.Id === createdPlanId);
    assertTrue(found, "Created crawl plan should appear in list");
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
        AuthType: "None",
        StartUrl: "https://example.com/updated",
        MaxDepth: 2,
        MaxParallelTasks: 1,
        CrawlDelayMs: 500,
        FollowLinks: true,
        FollowRedirects: true,
        RespectRobotsTxt: true,
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

  console.log("==========================================================");
  console.log("  AssistantHub JS SDK Test Suite");
  console.log("==========================================================");
  console.log("");
  console.log("  Base URL:  " + baseUrl);
  console.log("  API Key:   " + (apiKey ? "(set)" : "(none)"));
  console.log("");

  const client = new AssistantHubClient({ baseUrl, apiKey });
  const runner = new TestRunner();
  const totalStart = performance.now();

  try {
    await healthTests(runner, client);
    await tenantTests(runner, client);
    await assistantTests(runner, client);
    await collectionTests(runner, client);
    await documentTests(runner, client);
    await threadTests(runner, client, baseUrl, apiKey);
    await endpointTests(runner, client);
    await inferenceTests(runner, client);
    await evalTests(runner, client);
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
