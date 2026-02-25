# Changelog

## Current Version: v0.3.0

### Multi-Tenant Platform

- **Full multi-tenancy** -- Row-level tenant isolation across all entities (users, credentials, assistants, documents, ingestion rules, chat history, feedback). Each tenant operates in complete isolation within a shared deployment
- **Tenant management** -- CRUD API and dashboard view for creating, updating, and deleting tenants (global admin only)
- **Auto-provisioning** -- New tenants are automatically provisioned with a RecallDB tenant, default collection, S3 bucket, admin user, credential, and ingestion rule
- **Tenant deletion cascade** -- Deleting a tenant cleanly removes all child rows, S3 buckets, the RecallDB tenant, and the tenant record
- **Three-tier authorization** -- Global Admin (admin API keys or users with `IsAdmin=true`), Tenant Admin (`IsTenantAdmin`), and Tenant User roles with appropriate access controls throughout all handlers
- **Per-tenant S3 bucket isolation** -- Each tenant's S3 buckets are prefixed with `{tenantId}_` (e.g. `ten_abc123_default`). Bucket and object operations enforce tenant bucket prefix for non-global-admin users. Auto-provisioning creates a default bucket per tenant; deprovisioning deletes all tenant buckets
- **Per-tenant processing logs** -- Processing log files are namespaced by tenant ID in subdirectories, with backward-compatible fallback for pre-v0.3.0 logs
- **Per-tenant RecallDB mapping** -- Each AssistantHub tenant maps 1:1 to its own RecallDB tenant for vector isolation
- **Tenant-scoped API routes** -- Users and credentials are accessed via `/v1.0/tenants/{tenantId}/users` and `/v1.0/tenants/{tenantId}/credentials`
- **WhoAmI endpoint** -- `GET /v1.0/whoami` returns the current authentication context including tenant, role, and user details
- **Login with tenant context** -- Email/password authentication accepts optional `TenantId` parameter; dashboard login form includes Tenant ID field
- **Dashboard tenant awareness** -- Topbar shows tenant name and role badge; sidebar conditionally shows admin-only sections; 6 data views show Tenant column for global admins
- **Tenants dashboard view** -- Full CRUD interface with provisioning result modal showing auto-generated credentials
- **Breaking change** -- v0.3.0 is a new deployment; existing v0.2.0 databases require the manual migration script (`migrations/001_upgrade_to_v0.3.0.sql`) which adds `tenant_id` columns, creates the default tenant, promotes existing admins to tenant admins, and inserts default records. Scripts provided for SQLite, PostgreSQL, SQL Server, and MySQL
- **Protected records** -- Default tenant, admin user, and credential are marked `IsProtected = true` and cannot be deleted via API (returns 403). Per-tenant provisioned users and credentials are also protected. Protected status is visible and editable in the dashboard. Deactivate protected records by setting `Active = false` instead
- **Default credentials** -- Fresh deployments are pre-seeded with a default tenant, admin user (`admin@assistanthub` / `password`), bearer token (`default`), and ingestion rule. First-run auto-provisioning creates these records if no tenants exist
- **Database test suite updated** -- All 9 test classes (including new TenantTests) explicitly set TenantId and use tenant-scoped enumeration

### Other Changes

- **Neighbor chunk retrieval** -- Optionally retrieve surrounding chunks for each search match to provide broader document context to the model, configurable per assistant (0-10 neighbors)
- Removed `RecallDb.TenantId` from configuration (now derived from authenticated user context)
- Added `AdminApiKeys` and `DefaultTenant` settings sections

## v0.2.0

- **Initial release**
- **Multi-assistant platform** -- Create and manage multiple AI assistants, each with independent configuration, personality, knowledge base, and appearance
- **Automated document ingestion pipeline** -- Upload documents (PDF, text, HTML, and more); automatic text extraction via DocumentAtom, chunking and embedding via Partio, and storage in RecallDB
- **Ingestion rules** -- Define reusable ingestion configurations specifying target S3 buckets, RecallDB collections, chunking strategies, optional summarization, and embedding settings
- **Flexible search modes** -- Vector (semantic similarity), full-text (keyword matching), and hybrid search with tunable scoring weights for optimal retrieval
- **LLM-based retrieval gate** -- Optional per-assistant retrieval gate that classifies whether each user message requires new document retrieval or can be answered from existing conversation context
- **Conversation compaction** -- Automatic summarization of older messages when the conversation approaches the context window limit, preserving conversation continuity
- **Streaming chat responses** -- Real-time Server-Sent Events (SSE) streaming for token-by-token response delivery
- **Configurable inference endpoints** -- Support for Ollama (local) and OpenAI (cloud) inference providers, with per-assistant endpoint overrides via managed Partio endpoints
- **Document summarization** -- Optional pre-chunking or post-chunking summarization of document content using configurable completion endpoints
- **Public chat API** -- Unauthenticated OpenAI-compatible chat endpoint for embedding assistants into external applications
- **Feedback collection** -- Thumbs-up/thumbs-down feedback and free-text comments on assistant responses for quality monitoring
- **Chat history and performance metrics** -- Per-turn history with detailed timing measurements: retrieval duration, time to first token, time to last token, tokens per second, compaction duration, and more
- **Browser-based dashboard** -- Full management UI for assistants, documents, ingestion rules, endpoints, feedback, history, collections, buckets, users, and live chat testing
- **Multi-tenant user management** -- Admin and standard user roles with per-user assistant ownership
- **Multiple database backends** -- SQLite (default), PostgreSQL, SQL Server, and MySQL for the application database
- **One-command Docker deployment** -- Fully orchestrated Docker Compose stack with health checks, dependency ordering, and persistent volumes
- **Citation metadata in chat responses** -- When enabled per-assistant, the system instructs the model to cite source documents using bracket notation [1], [2] and returns a structured `citations` object in the response mapping references to source document names, IDs, relevance scores, and text excerpts
- **Citation document linking** -- Configurable `CitationLinkMode` setting (`None`, `Authenticated`, `Public`) that populates `download_url` on citation sources. All downloads are server-proxied (no direct S3 exposure). Public mode provides unauthenticated download gated by the assistant setting. Citation cards in the dashboard are clickable when a download URL is available
