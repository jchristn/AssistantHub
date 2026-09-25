# Slack Integration Plan

## Purpose

Add per-assistant Slack support to AssistantHub with the least disruption to the existing product surface:

- configuration lives in existing assistant settings
- Slack messages use the same chat execution rail as web chat
- deployment remains simple: no new service, no new required infrastructure
- docs, OpenAPI, and Postman stay complete

This document reflects the agreed implementation plan after technical review.

Status legend:

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete

## Final Decisions

### Scope decisions locked for v1

- Store Slack configuration in `assistant_settings`
- Add exactly 5 assistant settings fields:
  - `EnableSlack` `bool`
  - `SlackAppToken` `string`
  - `SlackBotToken` `string`
  - `SlackChannelId` `string`
  - `SlackMessagePrefix` `string`
- Do not add persisted `SlackLastConnectivity*` fields
- Add a nullable `origin` field to `chat_history` for observability with values such as `web`, `api`, `slack`
- Reuse the existing chat pipeline by extracting the core execution path from `ChatHandler` into a shared result-returning method/service
- Force non-streaming execution for Slack requests regardless of `AssistantSettings.Streaming`
- Use deterministic thread mapping based on `(assistant_id, slack_channel_id, slack_thread_ts_or_message_ts)`
- Do not add a separate `assistant_slack_threads` table in v1
- Consume `EasySlack` as a NuGet package at version `1.0.1`
- Treat `EasySlack` `1.0.1` threading support as a hard v1 dependency:
  - `ISlackConnector.SendMessageToChannelAsync(..., threadTimestamp, ...)`
  - `SlackMessageReceivedEventArgs.ThreadTimestamp`
- Slack verification must accept draft values from the UI before save
- Support both configured-channel conversations and direct-message conversations in v1
- Treat either the configured start-of-message indicator or an `@bot` mention as a valid trigger in channels
- Slack responses require outbound code-based shaping before `chat.postMessage`
- Persist canonical assistant response text to `chat_history`; never persist Slack-shaped transport text
- Use immediate worker restart on relevant settings changes plus a lightweight reconciler safety net

## Target User Experience

The Assistant Settings screen exposes a `Slack` section with:

- `Enable Slack`
- `App Token`
- `Bot Token`
- `Channel ID`
- `Start-of-Message Indicator`
- `Verify Connectivity`

Expected behavior:

- the user enters Slack values and clicks `Verify Connectivity` before saving
- when Slack is enabled with valid settings, AssistantHub maintains one Socket Mode connection for that assistant
- messages are accepted from either:
  - the configured channel when the message starts with the configured start-of-message indicator or mentions the bot
  - a direct message conversation with the bot
- accepted Slack messages are routed through the same retrieval, inference, compaction, citation, and history rails used by web chat
- replies are posted back into the originating Slack thread

## Architecture

### 1. Settings and storage

Use `assistant_settings` for Slack configuration. This preserves the current model boundary: assistant identity remains in `assistants`; runtime behavior remains in `assistant_settings`.

Add the five Slack fields above to:

- `src/AssistantHub.Core/Models/AssistantSettings.cs`
- all provider-specific `assistant_settings` table definitions
- all provider-specific assistant settings CRUD implementations
- settings API serialization/deserialization paths

Also add `origin` to `chat_history` across all supported DB providers and CRUD paths.

Notes:

- do not add token indexes
- do not add a separate Slack thread table in v1
- keep `origin` minimal; richer provider metadata can wait
- keep the existing 5-field model; do not add separate DM-only or mention-only settings in v1
- `SlackChannelId` remains the configured channel target for channel traffic; direct messages are additionally supported without introducing new settings fields

### 2. Shared chat execution

Do not duplicate `ChatHandler` orchestration and do not keep Slack on a separate inference path.

Extract the core execution flow from `src/AssistantHub.Server/Handlers/ChatHandler.cs` into a narrow shared execution method/service that:

- accepts assistant ID, messages, thread ID, and execution options
- supports `ForceNonStreaming = true`
- returns a result object containing:
  - canonical response text
  - citations
  - retrieval chunks / execution metadata as needed
  - thread ID / timing data as needed

Keep these concerns in `ChatHandler`:

- HTTP parsing
- SSE framing
- HTTP response formatting

Slack runtime will call the same shared execution path with Slack-specific options.

### 3. Three-tier response model

Slack output shaping must not contaminate persisted history.

The execution pipeline must distinguish:

1. `RawModelOutput`
2. `CanonicalResponseText`
3. `SlackTransportText` or chunked message parts

Rules:

- `CanonicalResponseText` is produced after model-agnostic cleanup such as bibliography stripping
- `CanonicalResponseText` is what gets persisted to `chat_history`
- `SlackTransportText` is derived only after canonical cleanup and only for outbound Slack delivery
- `SlackTransportText` is never persisted

This prevents compaction, replay, and cross-transport history from ingesting Slack-specific formatting.

### 4. Slack runtime

Add an assistant-scoped runtime layer under `src/AssistantHub.Server/Services/`:

- `ISlackAssistantConnectionManager`
- `SlackAssistantConnectionManager`
- `SlackAssistantWorker`

Responsibilities:

- enumerate Slack-enabled assistants at startup
- create one `EasySlack.SlackConnector` per enabled assistant
- validate startup configuration
- capture bot user ID from connection validation and suppress self-messages
- subscribe to inbound message events
- detect whether an inbound event is from the configured channel or a direct-message conversation
- filter by subtype and self-authored messages
- in configured channels, accept messages that either match the configured prefix or mention the bot
- in direct messages, accept the message without requiring the configured prefix
- derive a deterministic `thread_id`
- call shared chat execution
- shape and send Slack replies in-thread
- dispose cleanly on shutdown

### 5. EasySlack threading support

AssistantHub v1 should consume `EasySlack` from NuGet, pinned to version `1.0.1`.

Do not vendor, fork, or patch a local EasySlack project for the AssistantHub v1 implementation unless a later blocking defect forces that decision. The plan assumes the shipping integration uses the published package.

`EasySlack` `1.0.1` exposes the required threading primitives needed by AssistantHub:

- `ISlackConnector.SendMessageToChannelAsync(string channelId, string text, string? threadTimestamp = null, ...)`
- `SlackMessageReceivedEventArgs.ThreadTimestamp`

AssistantHub should treat these as required integration inputs, not as pending library work.

Threading rules:

- use `eventArgs.ThreadTimestamp ?? eventArgs.Timestamp` as the Slack conversation key
- derive deterministic AssistantHub `thread_id` from `(assistant_id, channel_id, slack_conversation_ts)`
- send replies with `SendMessageToChannelAsync(channelId, text, slackConversationTs, ...)`
- never fall back to top-level channel replies for threaded conversations
- support full thread continuity for both root messages and follow-up replies in channels and direct messages

### 6. Slack request behavior

Slack-originated requests must:

- force non-streaming mode
- preserve conversation continuity via deterministic thread mapping
- apply Slack-safe response shaping after canonical cleanup
- enforce a response-length policy for Slack limits
- support both configured-channel and DM-originated requests

Trigger rules:

- configured channels: process when the message starts with the configured prefix after normalization, or when the bot is mentioned
- direct messages: process regardless of prefix, while still allowing the prefix when the user chooses to include it

Response-length policy:

- chunk long replies into multiple Slack messages
- prefer readable chunking boundaries
- mark continued chunks clearly when needed

### 7. Slack output shaping

Slack shaping is a code path, not a prompt-only strategy.

Use a belt-and-suspenders approach:

- add a Slack-specific prompt suffix or execution hint requesting plain-text-safe output
- add outbound normalization code before `chat.postMessage`

Phase 1 shaping should be intentionally narrow and safe:

- normalize Markdown links to Slack-readable form where safe
- flatten Markdown headers to plain text
- clean up bold markers outside fenced code blocks
- preserve fenced code blocks and inline code
- avoid rewriting citation bracket patterns

Do not add a full Markdown parser/converter library in v1.

### 8. Verification flow

Add an authenticated verification route:

- `POST /v1.0/assistants/{assistantId}/settings/slack/verify`

The request must accept draft values from the settings form so users can test before save.

Verification behavior:

- validate bot token with `ValidateConnectionAsync()`
- validate channel with `GetChannelInfoAsync()`
- validate app-token-backed Socket Mode connectivity with a lightweight connect/probe if practical
- when verification cannot connect to Slack or cannot authenticate, emit an appropriate `_Logging.Warn(...)` entry without logging token values

Return a structured result with token-safe error details.

### 9. Runtime config changes

When assistant settings are updated:

- immediately restart only the affected Slack worker if relevant Slack fields changed
- start the worker if Slack became enabled
- stop the worker if Slack became disabled
- ignore unrelated settings changes

Add a lightweight background reconciler as a safety net:

- periodically compare expected enabled-worker state from DB to active workers
- repair drift from failed restart or out-of-band updates

Keep this reconciler simple and off the hot path.

## Work Plan

### A. Models and contracts

- `[x]` Add the 5 Slack settings fields to `src/AssistantHub.Core/Models/AssistantSettings.cs`
- `[x]` Update `AssistantSettings.FromDataRow(...)`
- `[x]` Add `origin` to the chat history model(s) and mapping code
- `[x]` Ensure defaults preserve backward compatibility
- `[x]` Update any model/API tests that assert full settings payloads

Acceptance:

- existing assistants continue to load without Slack configuration
- settings payloads support Slack-enabled assistants
- chat history can record `origin`

### B. Database and migrations

- `[x]` Add Slack columns to `assistant_settings` in:
  - `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs`
  - `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs`
  - `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs`
  - `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs`
- `[x]` Add `origin` to `chat_history` in the same provider definitions
- `[x]` Update assistant settings CRUD for all providers
- `[x]` Update chat history CRUD for all providers
- `[x]` Add one migration script under `migrations/` covering all new columns

Acceptance:

- fresh installs and upgrades work across SQLite, PostgreSQL, MySQL, and SQL Server
- no data loss for existing installs

### C. Settings handler validation

- `[x]` Update `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs`
- `[x]` Enforce:
  - `SlackAppToken` starts with `xapp-` when present
  - `SlackBotToken` starts with `xoxb-` when present
  - `SlackChannelId` is required when Slack is enabled
  - `SlackMessagePrefix` is required when Slack is enabled
- `[x]` Trim Slack fields
- `[x]` Match prefix case-insensitively after leading-whitespace normalization
- `[x]` Treat `@bot` mention detection as an alternate runtime trigger, not a replacement for `SlackMessagePrefix`

Acceptance:

- invalid Slack settings fail with clear `400` responses
- disabling Slack does not require tokens to remain populated

### D. Verification endpoint

- `[x]` Add `POST /v1.0/assistants/{assistantId}/settings/slack/verify`
- `[x]` Accept draft Slack values in the request body
- `[x]` Return structured verification results for bot token, channel, and Socket Mode connectivity
- `[x]` Ensure errors are actionable without exposing token values
- `[x]` Log verification connectivity/authentication failures with `_Logging.Warn(...)`

Acceptance:

- the dashboard can verify unsaved values
- verification does not start a long-lived worker
- verification failures are observable in server logs without leaking secrets

### E. Shared execution extraction

- `[x]` Extract the core execution path from `src/AssistantHub.Server/Handlers/ChatHandler.cs`
- `[x]` Implement a shared result-returning execution service/method
- `[x]` Add an execution option for `ForceNonStreaming`
- `[x]` Ensure persistence uses canonical response text, not raw model output and not Slack-shaped text

Acceptance:

- web chat and Slack use the same execution behavior
- Slack does not require pipeline duplication

### F. Slack runtime and worker management

- `[x]` Add `SlackAssistantConnectionManager` and per-assistant workers
- `[x]` Add or update the AssistantHub package reference to `EasySlack` NuGet `1.0.1`
- `[x]` Load all Slack-enabled assistants on startup
- `[x]` Capture bot user ID during validation/startup
- `[x]` Suppress self-messages
- `[x]` Accept direct messages to the bot
- `[x]` Filter configured-channel traffic to the configured channel
- `[x]` In configured-channel traffic, trigger on configured prefix or `@bot` mention
- `[x]` Dispose workers cleanly on shutdown
- `[x]` Add immediate restart on relevant settings changes
- `[x]` Add lightweight reconciler safety net

Acceptance:

- one assistant's Slack failure does not bring down others
- settings changes take effect without server restart
- direct-message conversations work without extra configuration beyond Slack enablement
- AssistantHub consumes `EasySlack` from NuGet `1.0.1`, not from a local project reference

### G. Slack routing and threading

- `[x]` Consume `SlackMessageReceivedEventArgs.ThreadTimestamp` for inbound thread detection
- `[x]` Use `eventArgs.ThreadTimestamp ?? eventArgs.Timestamp` as the Slack conversation key
- `[x]` Ensure inbound metadata exposes enough conversation type detail to distinguish configured-channel traffic from direct messages
- `[x]` Derive deterministic AssistantHub thread IDs from assistant/channel/thread identifiers
- `[x]` Use `SendMessageToChannelAsync(channelId, text, threadTimestamp, ...)` for thread-aware replies
- `[x]` Reply in-thread for both new and follow-up Slack conversations

Acceptance:

- Slack follow-ups map to the same AssistantHub conversation
- root messages and replies both remain in the same Slack thread
- channel noise is minimized
- direct-message conversations map cleanly without channel-only assumptions

### H. Slack output delivery

- `[x]` Add Slack-specific output shaping code after canonical cleanup
- `[x]` Preserve code blocks and avoid citation corruption
- `[x]` Add message chunking for Slack length limits
- `[x]` Send only Slack-shaped text to Slack

Acceptance:

- Slack replies are readable
- long outputs do not fail or become unusable

### I. Dashboard UI

- `[x]` Update `dashboard/src/views/AssistantSettingsView.jsx`
- `[x]` Add the Slack section with:
  - `Enable Slack`
  - `App Token`
  - `Bot Token`
  - `Channel ID`
  - `Start-of-Message Indicator`
  - `Verify Connectivity`
- `[x]` Use password-style or masked token fields
- `[x]` Ensure verification uses current in-form values
- `[x]` Add loading, success, and error states
- `[x]` Clarify in UI copy that `Channel ID` applies to channel traffic and that direct messages are also supported
- `[x]` Clarify in UI copy that either the configured indicator or an `@bot` mention can trigger the assistant in channels

Acceptance:

- the entire Slack setup flow can be completed from Assistant Settings

### J. Frontend API client and assets

- `[x]` Update `dashboard/src/utils/api.js`
- `[x]` Add Slack verify client method
- `[x]` Update `openapi.json`
- `[x]` Update `AssistantHub.postman_collection.json` with:
  - Slack settings examples
  - a dedicated `Verify Slack` request
  - environment placeholders for Slack tokens and channel ID

Acceptance:

- API consumers and Postman users can exercise the new feature without reverse-engineering payloads

### K. Logging and security

- `[x]` Add lifecycle logging for worker start/stop/restart/disconnect/reconnect
- `[~]` Log ignored-message reasons at useful granularity
- `[x]` Use `_Logging.Warn(...)` for Slack verification connect/auth failures
- `[x]` Never log token values
- `[x]` Ensure Slack settings are only returned from authenticated settings endpoints
- `[ ]` Document plaintext-at-rest limitations

Acceptance:

- Slack runtime is operable without exposing secrets

### L. Testing

- `[x]` Extend model tests for Slack settings defaults
- `[x]` Extend database tests for Slack settings persistence across providers
- `[x]` Extend chat history tests for `origin`
- `[ ]` Add handler tests for settings validation and verify endpoint
- `[ ]` Add service/runtime tests for:
  - worker startup/shutdown
  - self-message suppression
  - prefix filtering
  - `@bot` mention triggering
  - direct-message acceptance
  - deterministic thread mapping
  - root-message handling where `ThreadTimestamp` is null and `Timestamp` becomes the conversation key
  - reply-message handling where `ThreadTimestamp` is present and reused for continuity
  - threaded outbound replies using `SendMessageToChannelAsync(..., threadTimestamp, ...)`
  - worker restart on settings changes
  - Slack output shaping and chunking

Acceptance:

- Slack behavior is deterministic and covered at model, DB, handler, and service layers

### M. Documentation

- `[x]` Update `README.md`
- `[x]` Update `REST_API.md`
- `[x]` Update `openapi.json`
- `[x]` Document the `EasySlack` NuGet dependency at version `1.0.1`
- `[ ]` Document required Slack app configuration:
  - Socket Mode enabled
  - required scopes
  - event subscriptions
  - how to find channel ID
- `[ ]` Document start-of-message indicator semantics
- `[ ]` Document that `@bot` mentions also trigger the assistant in the configured channel
- `[ ]` Document that direct messages to the bot are supported
- `[ ]` Document thread reply behavior
- `[ ]` Document operational caveats:
  - app must be invited to private channels
  - one WebSocket connection per Slack-enabled assistant
  - tokens are stored in AssistantHub DB and should rely on deployment at-rest protections

Acceptance:

- an operator can configure and run the feature from docs alone

## Delivery Sequence

- `[x]` Phase 1: model, schema, CRUD, settings validation, `chat_history.origin`
- `[x]` Phase 2: verification endpoint, dashboard UI, API client, OpenAPI, Postman
- `[x]` Phase 3: shared chat execution extraction with canonical response persistence cleanup
- `[x]` Phase 4: Slack runtime, `EasySlack` NuGet `1.0.1`, deterministic thread mapping, self-message suppression, fully thread-aware reply posting
- `[x]` Phase 5: Slack output shaping and chunking
- `[~]` Phase 6: docs, tests, final validation

## Suggested File Touch List

- `src/AssistantHub.Core/Models/AssistantSettings.cs`
- `src/AssistantHub.Server/Handlers/AssistantSettingsHandler.cs`
- `src/AssistantHub.Server/Handlers/ChatHandler.cs`
- `src/AssistantHub.Server/AssistantHubServer.cs`
- `src/AssistantHub.Server/Services/AssistantChatService.cs` or equivalent shared execution service
- `src/AssistantHub.Server/Services/SlackAssistantConnectionManager.cs`
- `src/AssistantHub.Server/Services/SlackAssistantWorker.cs`
- `src/AssistantHub.Server/AssistantHub.Server.csproj`
- `src/AssistantHub.Core/Database/Sqlite/Queries/TableQueries.cs`
- `src/AssistantHub.Core/Database/Postgresql/Queries/TableQueries.cs`
- `src/AssistantHub.Core/Database/Mysql/Queries/TableQueries.cs`
- `src/AssistantHub.Core/Database/SqlServer/Queries/TableQueries.cs`
- `src/AssistantHub.Core/Database/Sqlite/Implementations/AssistantSettingsMethods.cs`
- `src/AssistantHub.Core/Database/Postgresql/Implementations/AssistantSettingsMethods.cs`
- `src/AssistantHub.Core/Database/Mysql/Implementations/AssistantSettingsMethods.cs`
- `src/AssistantHub.Core/Database/SqlServer/Implementations/AssistantSettingsMethods.cs`
- `src/AssistantHub.Core/Database/Sqlite/Implementations/ChatHistoryMethods.cs`
- `src/AssistantHub.Core/Database/Postgresql/Implementations/ChatHistoryMethods.cs`
- `src/AssistantHub.Core/Database/Mysql/Implementations/ChatHistoryMethods.cs`
- `src/AssistantHub.Core/Database/SqlServer/Implementations/ChatHistoryMethods.cs`
- `dashboard/src/views/AssistantSettingsView.jsx`
- `dashboard/src/utils/api.js`
- `src/Test.Database/Tests/AssistantSettingsTests.cs`
- `src/Test.Database/Tests/ChatHistoryTests.cs`
- `src/Test.Models/Tests/SettingsModelTests.cs`
- `README.md`
- `REST_API.md`
- `openapi.json`
- `AssistantHub.postman_collection.json`
- `migrations/<new migration>.sql`

## Final Acceptance Criteria

- `[ ]` A user can configure Slack on an assistant entirely from Assistant Settings
- `[ ]` A user can verify draft Slack credentials and channel settings before saving
- `[ ]` AssistantHub starts one Slack worker per Slack-enabled assistant
- `[ ]` Configured-channel Slack messages are ignored unless they come from the configured channel and either match the configured prefix or mention the bot
- `[ ]` Direct messages to the bot are processed without requiring channel configuration beyond Slack enablement
- `[ ]` Slack requests run through the same core chat execution path as web chat
- `[ ]` Slack execution is always non-streaming
- `[ ]` Slack replies are posted in-thread using `eventArgs.ThreadTimestamp ?? eventArgs.Timestamp` as the Slack conversation key
- `[ ]` AssistantHub consumes `EasySlack` via NuGet package `1.0.1`
- `[ ]` Threading is fully supported for both initial Slack messages and follow-up replies; no top-level fallback replies are used for threaded conversations
- `[ ]` Long Slack replies are chunked safely
- `[ ]` `chat_history` persists canonical response text plus `origin = slack` for Slack conversations
- `[ ]` Slack verification failures log appropriate `_Logging.Warn(...)` entries without exposing secrets
- `[ ]` Existing assistants and databases upgrade cleanly
- `[ ]` Docs, OpenAPI, Postman, and tests are updated end to end
