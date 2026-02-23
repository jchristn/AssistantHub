# Changelog

## Current Version: v0.2.0

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

## Previous Versions

Notes from previous versions will be placed here.
