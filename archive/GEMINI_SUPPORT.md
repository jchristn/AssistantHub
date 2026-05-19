# Gemini Support Plan

## Status

Implemented in AssistantHub on 2026-03-20.

- [x] Gemini added as a first-class native inference provider.
- [x] Gemini completion endpoint resolution added across chat, eval, and model-listing paths.
- [x] Dashboard endpoint creation/edit flows now pre-populate provider-specific defaults for Ollama, OpenAI, and Gemini.
- [x] Setup wizard now creates embedding/completion endpoints with full provider-aware default health-check payloads.
- [x] README, REST API docs, OpenAPI, and Postman examples updated to include Gemini.
- [x] Targeted .NET build/tests and dashboard production build completed successfully.

## Goal

Enable users to configure Gemini as:

- a backend embeddings provider for ingestion and prompt/query embeddings
- a backend inference provider for chat completions, summarization, evals, query rewrite, retrieval gate, reranking, thread compaction, and model listing where applicable

The end state is:

- Gemini can be created and managed as a Partio embedding endpoint and as a Partio completion endpoint through AssistantHub.
- Assistant settings and ingestion rules can point at Gemini-backed endpoints without special-case workarounds.
- AssistantHub's direct inference path also supports Gemini as a first-class provider.
- The dashboard, REST docs, OpenAPI, README, and Postman collection all describe and demonstrate Gemini correctly.

## Key Constraint

AssistantHub does **not** implement embeddings directly. Embedding endpoints and completion endpoints are proxied to Partio:

- [src/AssistantHub.Server/Handlers/EmbeddingEndpointHandler.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Handlers/EmbeddingEndpointHandler.cs)
- [src/AssistantHub.Server/Handlers/CompletionEndpointHandler.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Handlers/CompletionEndpointHandler.cs)

Historically, Gemini embedding support was blocked on **Partio** supporting a Gemini `ApiFormat` and Gemini endpoint execution semantics. That Partio work now exists, but AssistantHub still depends on it for managed embedding/completion endpoint behavior.

## Partio Review Update

Partio now implements Gemini as a real upstream format. Relevant files reviewed:

- [src/Partio.Core/Enums/ApiFormatEnum.cs](/C:/Code/partio/partio/src/Partio.Core/Enums/ApiFormatEnum.cs)
- [src/Partio.Core/ThirdParty/GeminiEmbeddingClient.cs](/C:/Code/partio/partio/src/Partio.Core/ThirdParty/GeminiEmbeddingClient.cs)
- [src/Partio.Core/ThirdParty/GeminiCompletionClient.cs](/C:/Code/partio/partio/src/Partio.Core/ThirdParty/GeminiCompletionClient.cs)
- [src/Partio.Server/PartioServer.cs](/C:/Code/partio/partio/src/Partio.Server/PartioServer.cs)
- [src/Partio.Core/Models/EmbeddingEndpoint.cs](/C:/Code/partio/partio/src/Partio.Core/Models/EmbeddingEndpoint.cs)
- [src/Partio.Core/Models/CompletionEndpoint.cs](/C:/Code/partio/partio/src/Partio.Core/Models/CompletionEndpoint.cs)
- [src/Partio.Server/Services/EmbeddingHealthCheckService.cs](/C:/Code/partio/partio/src/Partio.Server/Services/EmbeddingHealthCheckService.cs)
- [src/Partio.Server/Services/CompletionHealthCheckService.cs](/C:/Code/partio/partio/src/Partio.Server/Services/CompletionHealthCheckService.cs)

Confirmed Partio behavior:

- `ApiFormat = Gemini` is persisted and enumerated for both embedding and completion endpoints.
- Partio creates Gemini clients through `GeminiEmbeddingClient` and `GeminiCompletionClient`.
- Those clients are backed by `PolyPrompt.Clients.GeminiClient`.
- Gemini embedding execution is normalized to Partio's existing embedding response shape.
- Gemini completion execution is normalized to Partio's existing completion/summarization path.
- Gemini health checks default to `{Endpoint}/v1beta/models`.
- Gemini health checks send auth via `x-goog-api-key`, not Bearer.
- Partio auto-enables authenticated health checks for Gemini when an API key is present.

Result:

- The original Partio blocker has been cleared.
- AssistantHub work can now proceed against a known Partio Gemini contract instead of assumptions.

## Current State Summary

### AssistantHub direct inference

AssistantHub native inference currently supports only `OpenAI` and `Ollama`:

- [src/AssistantHub.Core/Enums/InferenceProviderEnum.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Enums/InferenceProviderEnum.cs)
- [src/AssistantHub.Core/Services/InferenceService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/InferenceService.cs)
- [src/AssistantHub.Server/Services/AssistantChatService.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Services/AssistantChatService.cs)
- [src/AssistantHub.Core/Services/EvalService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/EvalService.cs)
- [src/AssistantHub.Server/Handlers/InferenceHandler.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Handlers/InferenceHandler.cs)

### Embeddings and summarization path

Embeddings, ingestion summarization, and query embeddings go through Partio using endpoint IDs:

- [src/AssistantHub.Core/Services/IngestionService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/IngestionService.cs)
- [src/AssistantHub.Core/Services/RetrievalService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/RetrievalService.cs)

### Dashboard

The dashboard hard-codes `ApiFormat` options around `OpenAI` and `Ollama` in multiple places:

- [dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx)
- [dashboard/src/components/modals/InferenceEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/InferenceEndpointFormModal.jsx)
- [dashboard/src/components/modals/ConfigurationFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/ConfigurationFormModal.jsx)
- [dashboard/src/components/SetupWizard.jsx](/C:/Code/AssistantHub/dashboard/src/components/SetupWizard.jsx)

### Docs and examples

Gemini is not documented as a supported provider today:

- [README.md](/C:/Code/AssistantHub/README.md)
- [REST_API.md](/C:/Code/AssistantHub/REST_API.md)
- [openapi.json](/C:/Code/AssistantHub/openapi.json)
- [postman/AssistantHub.postman_collection.json](/C:/Code/AssistantHub/postman/AssistantHub.postman_collection.json)

## Scope Decision

### [1] Mandatory

- Add Gemini as a first-class inference provider in AssistantHub.
- Allow Gemini `ApiFormat` to flow through endpoint CRUD, dashboard forms, setup wizard, and docs.
- Ensure assistant-level completion endpoint resolution maps Gemini endpoints correctly.
- Ensure ingestion/query embedding workflows can target Gemini-backed embedding endpoints through Partio.
- Update user-facing documentation and Postman examples.

### [2] Strongly Recommended

- Add explicit Gemini health-check defaults in the dashboard.
- Add model-listing support for Gemini in `/v1.0/models`.
- Add tests for provider parsing, provider capability flags, and endpoint resolution.

### [3] Optional / Factory Defaults

- Add Gemini examples to Docker/factory config files.
- Add environment guidance for `GEMINI_API_KEY`.
- Add default Gemini endpoints in Partio factory config if the project wants cloud-first defaults.

## Architecture Notes

There are two integration layers:

1. Partio-managed endpoints
   Embedding endpoints: required for ingestion and retrieval embeddings.
   Completion endpoints: used by ingestion summarization and can also back assistant completion endpoints.

2. AssistantHub native inference provider
   Used by `InferenceService` and everything built on top of it.
   Requires direct Gemini HTTP integration for chat completions and model listing.

Gemini support is complete only when both layers are addressed.

## Confirmed Partio Contract To Use In AssistantHub

### Managed endpoint format

- Embedding endpoint `ApiFormat`: `Gemini`
- Completion endpoint `ApiFormat`: `Gemini`

### Health checks

- Default URL: `{Endpoint}/v1beta/models`
- Default auth: `x-goog-api-key: {ApiKey}`
- Default cadence: Gemini follows Partio's non-Ollama health-check defaults, not Ollama's fast local cadence

### Runtime behavior

- AssistantHub does not need to understand Gemini request/response shapes for ingestion embeddings or ingestion summarization when those flows go through Partio-managed endpoints.
- AssistantHub does still need native Gemini support in `InferenceService` for direct inference provider usage.

## Work Plan

### Phase 0 - Confirm API Strategy

- [ ] Decide whether AssistantHub native Gemini inference should mirror Partio and use Google's Gemini API semantics directly, or whether it should target a broader abstraction.
- [ ] Decide whether AssistantHub native support is:
  - Google Gemini API only, or
  - Google Gemini API plus Vertex AI, or
  - Google Gemini API first with Vertex AI explicitly out of scope
- [ ] Document the native AssistantHub Gemini contract for:
  - model listing
  - non-streaming chat
  - streaming chat
- [ ] Decide provider naming:
  - `Gemini` for AssistantHub `InferenceProviderEnum`
  - `Gemini` for Partio `ApiFormat`
- [ ] Decide model-name expectations:
  - raw Gemini names like `gemini-2.5-flash`
  - embedding names like `text-embedding-004` or the chosen current Gemini embedding model
- [ ] Decide auth strategy:
  - match Partio's Gemini convention where practical
  - explicitly define whether AssistantHub native inference will also use `x-goog-api-key`

Acceptance criteria:

- A developer can implement AssistantHub native Gemini inference without guessing request URLs, auth placement, or streaming format.

### Phase 1 - Partio Dependency Work

- [x] Update Partio embedding endpoint model validation to accept `ApiFormat = Gemini`.
- [x] Update Partio completion endpoint model validation to accept `ApiFormat = Gemini`.
- [x] Implement Gemini embedding execution in Partio.
- [x] Implement Gemini completion execution in Partio for summarization and other completion endpoint consumers.
- [x] Implement Gemini-aware endpoint health checks.
- [x] Preserve `ApiFormat = Gemini` through Partio persistence and enumeration.
- [x] Add at least baseline Partio coverage proving Gemini `ApiFormat` is preserved in automated tests.
- [ ] Verify with a real Gemini-backed endpoint in Partio if not already done outside this repo.

AssistantHub impact:

- AssistantHub can already proxy endpoint CRUD to Partio.
- The remaining AssistantHub work is now mostly:
  - UI/provider option updates
  - local `ApiFormat` to provider mapping fixes
  - native Gemini inference support
  - docs/examples/tests

Risk:

- Remaining risk is now contract drift between AssistantHub assumptions and the finalized Partio Gemini behavior.

### Phase 2 - AssistantHub Core Provider Support

- [ ] Add `Gemini` to [src/AssistantHub.Core/Enums/InferenceProviderEnum.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Enums/InferenceProviderEnum.cs).
- [ ] Update any enum serialization tests in [src/Test.Models/Tests/EnumTests.cs](/C:/Code/AssistantHub/src/Test.Models/Tests/EnumTests.cs).
- [ ] Update default configuration UI/provider dropdowns to include Gemini.
- [ ] Verify settings JSON round-trips `Provider = "Gemini"` correctly in:
  - [src/AssistantHub.Core/Settings/InferenceSettings.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Settings/InferenceSettings.cs)
  - [docker/assistanthub/assistanthub.json](/C:/Code/AssistantHub/docker/assistanthub/assistanthub.json) if examples are changed

Implementation notes:

- No AssistantHub database migration appears necessary for provider support itself.
- Assistant settings and ingestion rules already store endpoint IDs and model names, not provider-specific schema.

### Phase 3 - Native AssistantHub Gemini Inference

Files primarily affected:

- [src/AssistantHub.Core/Services/InferenceService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/InferenceService.cs)
- [src/AssistantHub.Server/Handlers/InferenceHandler.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Handlers/InferenceHandler.cs)
- [src/AssistantHub.Server/Services/AssistantChatService.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Services/AssistantChatService.cs)
- [src/AssistantHub.Core/Services/EvalService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/EvalService.cs)

- [ ] Add `Gemini` branches to `InferenceService.ListModelsAsync`.
- [ ] Add `Gemini` branches to all `GenerateResponseAsync` overloads.
- [ ] Add `Gemini` branch to `GenerateResponseStreamingAsync`.
- [ ] Implement a Gemini-specific non-streaming message path.
- [ ] Implement a Gemini-specific streaming parser.
- [ ] Implement Gemini model listing.
- [ ] Keep `PullModelAsync` and `DeleteModelAsync` unsupported for Gemini unless the provider actually supports equivalent semantics.
- [ ] Ensure `IsPullSupported` and `IsDeleteSupported` remain `false` for Gemini.
- [ ] Update logging/error text so unsupported-provider messages include Gemini only where appropriate.

Specific call sites to verify after implementation:

- [ ] Standard assistant chat completions
- [ ] Streaming chat completions
- [ ] Retrieval gate inference
- [ ] Query rewrite inference
- [ ] Reranking inference
- [ ] Conversation compaction summarization
- [ ] Eval run generation and judge calls

Acceptance criteria:

- Any code path that currently works with OpenAI/Ollama also works with Gemini when the assistant resolves to a Gemini provider or the global provider is Gemini.
- Auth/header behavior is explicit and consistent with the chosen Gemini native implementation.

### Phase 4 - Endpoint Resolution and Provider Mapping

Current code often maps Partio completion endpoint `ApiFormat` using OpenAI-vs-default-to-Ollama logic. That must be fixed.

Files:

- [src/AssistantHub.Server/Services/AssistantChatService.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Services/AssistantChatService.cs)
- [src/AssistantHub.Core/Services/EvalService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/EvalService.cs)
- [src/AssistantHub.Server/Handlers/InferenceHandler.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Handlers/InferenceHandler.cs)

- [ ] Replace binary provider mapping logic with explicit mapping:
  - `OpenAI` -> `InferenceProviderEnum.OpenAI`
  - `Ollama` -> `InferenceProviderEnum.Ollama`
  - `Gemini` -> `InferenceProviderEnum.Gemini`
- [ ] Avoid defaulting unknown values silently to Ollama.
- [ ] Decide whether unknown `ApiFormat` values should:
  - fail closed with warnings, or
  - fall back to configured global provider
- [ ] Add unit tests for endpoint resolution logic.

Acceptance criteria:

- A completion endpoint returned by Partio with `ApiFormat = Gemini` is resolved as Gemini everywhere.
- AssistantHub no longer silently interprets unknown formats as Ollama.

### Phase 5 - Embeddings Flow Validation

AssistantHub embeddings are endpoint-ID based, but Gemini still needs validation at all usage points.

Files:

- [src/AssistantHub.Core/Services/IngestionService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/IngestionService.cs)
- [src/AssistantHub.Core/Services/RetrievalService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/RetrievalService.cs)

- [ ] Confirm ingestion processing requests do not assume Ollama/OpenAI-specific embedding response shapes beyond what Partio already normalizes.
- [ ] Confirm single-chunk embed requests continue to work when Partio executes a Gemini embedding endpoint.
- [ ] Confirm query embedding requests continue to work when `EmbeddingEndpointId` points to Gemini.
- [ ] Confirm no dimensionality assumptions are hard-coded in AssistantHub collection creation or retrieval logic.
- [ ] Decide whether docs should instruct users to create collections with a Gemini-compatible dimensionality explicitly.

Likely result:

- No code change may be needed in AssistantHub runtime once Partio supports Gemini embeddings.
- Documentation and UI still need updates so users can choose a Gemini embedding endpoint intentionally.

### Phase 6 - Dashboard and UX

Files:

- [dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx)
- [dashboard/src/components/modals/InferenceEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/InferenceEndpointFormModal.jsx)
- [dashboard/src/components/modals/ConfigurationFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/ConfigurationFormModal.jsx)
- [dashboard/src/components/SetupWizard.jsx](/C:/Code/AssistantHub/dashboard/src/components/SetupWizard.jsx)

- [ ] Add `Gemini` to embedding endpoint format options.
- [ ] Add `Gemini` to inference endpoint format options.
- [ ] Add `Gemini` to global inference provider configuration options.
- [ ] Update helper text/tooltips to say "Ollama, OpenAI, or Gemini" where applicable.
- [ ] Add Gemini-aware default health-check URL logic matching Partio: `{Endpoint}/v1beta/models`.
- [ ] Ensure dashboard help text reflects Gemini auth using API key semantics rather than Bearer-only wording.
- [ ] Ensure create/edit modals preserve existing Gemini endpoints without resetting format-specific defaults.
- [ ] Update Setup Wizard to allow Gemini for both embedding and inference endpoint creation.
- [ ] Remove current inconsistency where the wizard shows `VoyageAI` for embeddings but the main modal does not.

UX acceptance criteria:

- An admin can create a Gemini embedding endpoint, a Gemini completion endpoint, and select them in assistant settings and ingestion rules without manual JSON edits.

### Phase 7 - Health Checks and Connectivity

Files:

- [dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx)
- [dashboard/src/components/modals/InferenceEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/InferenceEndpointFormModal.jsx)
- [src/AssistantHub.Server/AssistantHubServer.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/AssistantHubServer.cs)

- [ ] Add a Gemini default health-check URL strategy in dashboard forms.
- [ ] Make sure the strategy matches Partio exactly: `x-goog-api-key` auth and `/v1beta/models` probe target.
- [ ] Decide whether startup connectivity checks in AssistantHub should keep the generic "Inference" probe or become provider-aware.
- [ ] Review log messages like `Inference (Ollama)` in connectivity validation and rename them to provider-neutral text if Gemini can be the configured global provider.
- [ ] Verify health-check service behavior when Gemini endpoints are created/updated/deleted through Partio.

Note:

- Endpoint health itself is primarily owned by Partio for managed endpoints.
- AssistantHub startup inference connectivity is separate and uses the configured global inference settings.

### Phase 8 - API Contracts, Docs, and Postman

Files:

- [README.md](/C:/Code/AssistantHub/README.md)
- [REST_API.md](/C:/Code/AssistantHub/REST_API.md)
- [openapi.json](/C:/Code/AssistantHub/openapi.json)
- [postman/AssistantHub.postman_collection.json](/C:/Code/AssistantHub/postman/AssistantHub.postman_collection.json)
- [CHANGELOG.md](/C:/Code/AssistantHub/CHANGELOG.md) if this ships in a release

- [ ] Update README feature lists to include Gemini anywhere inference/endpoint support is enumerated.
- [ ] Update architecture text that currently says embeddings are computed via Ollama only.
- [ ] Update deployment/configuration examples to show how Gemini API keys and models are configured.
- [ ] Document the split responsibility:
  - embeddings/completion endpoints are managed through Partio
  - direct inference provider is configured in AssistantHub
- [ ] Document the confirmed Partio Gemini behavior:
  - `ApiFormat = Gemini`
  - health checks target `/v1beta/models`
  - Gemini health auth uses `x-goog-api-key`
- [ ] Update REST endpoint examples for:
  - `PUT /v1.0/endpoints/embedding`
  - `PUT /v1.0/endpoints/completion`
  - `GET /v1.0/models`
  - assistant settings examples that reference inference endpoint IDs
  - ingestion rule examples that reference embedding and summarization endpoint IDs
- [ ] Update OpenAPI schema descriptions for `ApiFormat`, provider enums, model ownership text, and provider support notes.
- [ ] Update Postman request bodies to include Gemini examples for both embedding and completion endpoints.
- [ ] Fix Postman model pull request body if needed to match server expectations.

Documentation acceptance criteria:

- A user can follow docs alone to configure Gemini for ingestion embeddings, query embeddings, summarization, and chat.

### Phase 9 - Tests

Files likely affected:

- [src/Test.Models/Tests/EnumTests.cs](/C:/Code/AssistantHub/src/Test.Models/Tests/EnumTests.cs)
- [src/Test.Services/Tests/InferenceServiceTests.cs](/C:/Code/AssistantHub/src/Test.Services/Tests/InferenceServiceTests.cs)
- add new tests near endpoint resolution call sites if the project already has suitable coverage locations

- [ ] Add enum coverage for `InferenceProviderEnum.Gemini`.
- [ ] Add `IsPullSupported == false` test for Gemini.
- [ ] Add `IsDeleteSupported == false` test for Gemini.
- [ ] Add `PullModelAsync` unsupported-provider test for Gemini.
- [ ] Add model-listing test coverage for Gemini error handling.
- [ ] Add endpoint resolution tests proving `ApiFormat = Gemini` maps correctly.
- [ ] Add chat/inference tests using mocked Gemini responses for:
  - non-streaming messages
  - streaming messages
  - malformed-response handling
- [ ] Add integration coverage, if feasible, for assistant chat using a Gemini-configured completion endpoint.
- [ ] Add regression tests ensuring Gemini Partio endpoints do not get misclassified as Ollama during endpoint resolution.

### Phase 10 - Factory/Bootstrap Defaults

Files:

- [docker/partio/partio.json](/C:/Code/AssistantHub/docker/partio/partio.json)
- [docker/assistanthub/assistanthub.json](/C:/Code/AssistantHub/docker/assistanthub/assistanthub.json)

- [ ] Decide whether factory defaults stay Ollama-first or include Gemini examples.
- [ ] If adding examples, update Partio default embedding/completion endpoints to optionally show Gemini samples.
- [ ] If adding examples, update AssistantHub default inference config to optionally show Gemini.
- [ ] Document secret-management expectations for Gemini API keys in Docker deployments.

Recommendation:

- Keep factory runtime defaults Ollama-first unless the product is intentionally moving to cloud-first behavior.
- Add Gemini as a documented alternative example instead of changing bootstrap behavior by default.

## Suggested Implementation Order

- [ ] 1. Finalize Gemini API contract and provider naming.
- [x] 2. Implement Partio Gemini endpoint support.
- [ ] 3. Add `InferenceProviderEnum.Gemini` and direct Gemini inference support in AssistantHub.
- [ ] 4. Fix endpoint resolution mapping everywhere that currently defaults to Ollama.
- [ ] 5. Update dashboard provider options and health-check defaults to match Partio.
- [ ] 6. Add tests.
- [ ] 7. Update README, REST docs, OpenAPI, and Postman.
- [ ] 8. Optionally update factory/default config examples.

## Likely Code Changes by File

### AssistantHub backend

- [ ] [src/AssistantHub.Core/Enums/InferenceProviderEnum.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Enums/InferenceProviderEnum.cs)
- [ ] [src/AssistantHub.Core/Services/InferenceService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/InferenceService.cs)
- [ ] [src/AssistantHub.Server/Handlers/InferenceHandler.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Handlers/InferenceHandler.cs)
- [ ] [src/AssistantHub.Server/Services/AssistantChatService.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/Services/AssistantChatService.cs)
- [ ] [src/AssistantHub.Core/Services/EvalService.cs](/C:/Code/AssistantHub/src/AssistantHub.Core/Services/EvalService.cs)
- [ ] [src/AssistantHub.Server/AssistantHubServer.cs](/C:/Code/AssistantHub/src/AssistantHub.Server/AssistantHubServer.cs)

### AssistantHub frontend

- [ ] [dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/EmbeddingEndpointFormModal.jsx)
- [ ] [dashboard/src/components/modals/InferenceEndpointFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/InferenceEndpointFormModal.jsx)
- [ ] [dashboard/src/components/modals/ConfigurationFormModal.jsx](/C:/Code/AssistantHub/dashboard/src/components/modals/ConfigurationFormModal.jsx)
- [ ] [dashboard/src/components/SetupWizard.jsx](/C:/Code/AssistantHub/dashboard/src/components/SetupWizard.jsx)

### Docs and examples

- [ ] [README.md](/C:/Code/AssistantHub/README.md)
- [ ] [REST_API.md](/C:/Code/AssistantHub/REST_API.md)
- [ ] [openapi.json](/C:/Code/AssistantHub/openapi.json)
- [ ] [postman/AssistantHub.postman_collection.json](/C:/Code/AssistantHub/postman/AssistantHub.postman_collection.json)
- [ ] [docker/partio/partio.json](/C:/Code/AssistantHub/docker/partio/partio.json)
- [ ] [docker/assistanthub/assistanthub.json](/C:/Code/AssistantHub/docker/assistanthub/assistanthub.json)

### Tests

- [ ] [src/Test.Models/Tests/EnumTests.cs](/C:/Code/AssistantHub/src/Test.Models/Tests/EnumTests.cs)
- [ ] [src/Test.Services/Tests/InferenceServiceTests.cs](/C:/Code/AssistantHub/src/Test.Services/Tests/InferenceServiceTests.cs)

## Open Questions

- [ ] Should Gemini support target only Google's hosted API, or also Vertex AI?
- [ ] Should Gemini model listing be supported if the upstream API does not expose a simple equivalent to current OpenAI/Ollama flows?
- [x] What is the canonical health-check endpoint for Gemini in this product?
  Answer from Partio: `{Endpoint}/v1beta/models`, with `x-goog-api-key` when auth is enabled.
- [ ] Should AssistantHub continue to support direct-provider configuration and Partio-managed completion endpoints in parallel, or should one become preferred?
- [ ] Should embedding dimensionality guidance be surfaced in the collection creation flow to reduce mismatches between model vectors and RecallDB collections?

## Completion Checklist

- [x] Partio accepts `ApiFormat = Gemini` for embedding endpoints.
- [x] Partio accepts `ApiFormat = Gemini` for completion endpoints.
- [ ] AssistantHub native inference supports `InferenceProviderEnum.Gemini`.
- [ ] Assistant endpoint resolution correctly maps Gemini completion endpoints.
- [ ] Dashboard can create/edit/select Gemini embedding and completion endpoints.
- [ ] Ingestion works with a Gemini embedding endpoint.
- [ ] Query retrieval works with a Gemini embedding endpoint.
- [ ] Chat completions work with Gemini.
- [ ] Summarization works with Gemini completion endpoints.
- [ ] Eval, reranking, retrieval gate, query rewrite, and compaction work with Gemini.
- [ ] README, REST API docs, OpenAPI, and Postman are updated.
- [ ] Tests cover Gemini provider parsing and runtime behavior.

## Recommended Non-Goals

- Do not introduce database schema changes unless a real Gemini-specific persistence need appears.
- Do not change existing Ollama defaults unless product direction explicitly calls for it.
- Do not expose Gemini in the UI before Partio can actually execute Gemini endpoints unless the UI clearly labels support as pending.
