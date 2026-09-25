namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for unauthenticated, public assistant routes (chat, feedback, threads, documents, and filters).
    /// </summary>
    public static class PublicAssistantApiDocs
    {
        #region Private-Members

        private const string _Tag = "Assistant Public APIs";

        private const string _ThreadIdHeader = "X-Thread-ID";

        private const string _ChatCompletionId = "chatcmpl-01JH3ZA4B6C8D0E2F4G6H8J0K2";

        private const string _FeedbackId = "afb_01JH3ZB7C9D1E3F5G7H9J1K3M5";

        private const string _ChatHistoryId = "chist_01JH3ZC2D4E6F8G0H2J4K6M8N0";

        private const string _Model = "gpt-4o";

        private const long _CreatedUnix = 1768498200;

        private const string _SseExample =
            "data: {\"id\":\"" + _ChatCompletionId + "\",\"object\":\"chat.completion.chunk\",\"created\":1768498200,\"model\":\"gpt-4o\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\"}}]}\n\n" +
            "event: assistant.tool_call.started\n" +
            "data: {\"event_type\":\"assistant.tool_call.started\",\"tool_call_id\":\"call_search\",\"tool_name\":\"collection_search\",\"display_label\":\"Searching collection\",\"status_code\":\"tool_started\",\"iteration\":1,\"sequence_number\":1,\"summary\":\"Searching collection running.\"}\n\n" +
            "data: {\"id\":\"" + _ChatCompletionId + "\",\"object\":\"chat.completion.chunk\",\"created\":1768498200,\"model\":\"gpt-4o\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"To reset your password,\"}}]}\n\n" +
            "data: {\"id\":\"" + _ChatCompletionId + "\",\"object\":\"chat.completion.chunk\",\"created\":1768498200,\"model\":\"gpt-4o\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\" open Settings > Security [1].\"}}]}\n\n" +
            "data: {\"id\":\"" + _ChatCompletionId + "\",\"object\":\"chat.completion.chunk\",\"created\":1768498200,\"model\":\"gpt-4o\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":412,\"completion_tokens\":14,\"total_tokens\":426,\"context_window\":8192}}\n\n" +
            "data: [DONE]\n\n";

        private static List<ChatCompletionMessage> ExampleConversation()
        {
            return new List<ChatCompletionMessage>
            {
                new ChatCompletionMessage { Role = "user", Content = "What is machine learning?" },
                new ChatCompletionMessage { Role = "assistant", Content = "Machine learning is a subset of artificial intelligence that learns patterns from data." },
                new ChatCompletionMessage { Role = "user", Content = "How does supervised learning work?" }
            };
        }

        private static ChatCompletionRequest ExampleChatRequest()
        {
            return new ChatCompletionRequest
            {
                Model = _Model,
                Messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage { Role = "user", Content = "How do I reset my password?" }
                },
                Stream = false,
                Temperature = 0.7,
                TopP = 1.0,
                MaxTokens = 4096,
                AttachedDocumentIds = new List<string> { ApiExamples.DocumentId },
                LocalAttachments = new List<ChatLocalAttachment>
                {
                    new ChatLocalAttachment
                    {
                        Name = "notes.txt",
                        ContentType = "text/plain",
                        Base64Content = "VGhpcyBpcyBhIGxvY2FsIGZpbGUu"
                    }
                },
                MetadataFilter = new ChatMetadataFilter
                {
                    RequiredLabels = new List<string> { "support" },
                    ExcludedLabels = new List<string> { "draft" },
                    RequiredTags = new List<ChatTagCondition>
                    {
                        new ChatTagCondition { Key = "department", Condition = "Equals", Value = "customer-success" }
                    },
                    ExcludedTags = new List<ChatTagCondition>
                    {
                        new ChatTagCondition { Key = "status", Condition = "Equals", Value = "archived" }
                    }
                }
            };
        }

        private static ChatCompletionRequest ExampleCompactRequest()
        {
            List<ChatCompletionMessage> messages = ExampleConversation();
            messages.Add(new ChatCompletionMessage { Role = "assistant", Content = "Supervised learning trains a model on labeled examples so it can predict labels for new inputs." });

            return new ChatCompletionRequest
            {
                Model = _Model,
                Messages = messages,
                Temperature = 0.7,
                MaxTokens = 4096
            };
        }

        private static ChatCompletionRequest ExampleGenerateRequest()
        {
            return new ChatCompletionRequest
            {
                Model = _Model,
                Messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage { Role = "user", Content = "What is the capital of France?" },
                    new ChatCompletionMessage { Role = "assistant", Content = "The capital of France is Paris." },
                    new ChatCompletionMessage { Role = "user", Content = "Generate a short title (max 6 words) for this conversation. Reply with ONLY the title text, nothing else." }
                },
                Temperature = 0.7,
                TopP = 1.0,
                MaxTokens = 64
            };
        }

        private static AssistantDocumentSelectionItem ExampleDocumentItem()
        {
            return new AssistantDocumentSelectionItem
            {
                Id = ApiExamples.DocumentId,
                Name = "Account Security Guide",
                OriginalFilename = "account-security-guide.pdf",
                ContentType = "application/pdf",
                SizeBytes = 248312,
                SourceUrl = null,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static ChatCompletionResponse ExampleChatResponse()
        {
            return new ChatCompletionResponse
            {
                Id = _ChatCompletionId,
                Object = "chat.completion",
                Created = _CreatedUnix,
                Model = _Model,
                Choices = new List<ChatCompletionChoice>
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Message = new ChatCompletionMessage
                        {
                            Role = "assistant",
                            Content = "To reset your password, open Settings > Security and select Reset Password [1]."
                        },
                        FinishReason = "stop"
                    }
                },
                Usage = new ChatCompletionUsage
                {
                    PromptTokens = 412,
                    CompletionTokens = 18,
                    TotalTokens = 430,
                    ContextWindow = 8192
                },
                Retrieval = new ChatCompletionRetrieval
                {
                    CollectionId = ApiExamples.CollectionId,
                    DurationMs = 42.7,
                    ChunksReturned = 1,
                    AttachedDocumentIds = new List<string> { ApiExamples.DocumentId },
                    AttachedDocuments = new List<AssistantDocumentSelectionItem> { ExampleDocumentItem() },
                    DocumentFilterApplied = true,
                    Chunks = new List<RetrievalChunk>
                    {
                        new RetrievalChunk
                        {
                            DocumentId = ApiExamples.DocumentId,
                            Score = 0.87,
                            Content = "To reset your password, open Settings > Security and select Reset Password.",
                            Position = 3
                        }
                    }
                },
                Citations = new ChatCompletionCitations
                {
                    Sources = new List<CitationSource>
                    {
                        new CitationSource
                        {
                            Index = 1,
                            SourceType = "document",
                            DocumentId = ApiExamples.DocumentId,
                            DocumentName = "Account Security Guide",
                            ContentType = "application/pdf",
                            Score = 0.87,
                            Excerpt = "To reset your password, open Settings > Security and select Reset Password.",
                            DownloadUrl = "/v1.0/assistants/" + ApiExamples.AssistantId + "/documents/" + ApiExamples.DocumentId + "/download"
                        }
                    },
                    ReferencedIndices = new List<int> { 1 }
                }
            };
        }

        private static ChatCompletionResponse ExampleGenerateResponse()
        {
            return new ChatCompletionResponse
            {
                Id = _ChatCompletionId,
                Object = "chat.completion",
                Created = _CreatedUnix,
                Model = _Model,
                Choices = new List<ChatCompletionChoice>
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Message = new ChatCompletionMessage { Role = "assistant", Content = "European Capital Cities" },
                        FinishReason = "stop"
                    }
                },
                Usage = new ChatCompletionUsage
                {
                    PromptTokens = 50,
                    CompletionTokens = 5,
                    TotalTokens = 55,
                    ContextWindow = 8192
                }
            };
        }

        private static AssistantFeedback ExampleFeedback()
        {
            return new AssistantFeedback
            {
                Id = _FeedbackId,
                TenantId = ApiExamples.TenantId,
                AssistantId = ApiExamples.AssistantId,
                UserMessage = "How do I reset my password?",
                AssistantResponse = "To reset your password, open Settings > Security and select Reset Password.",
                Rating = FeedbackRatingEnum.ThumbsUp,
                FeedbackText = "This was exactly what I needed!",
                MessageHistory = "[{\"role\":\"user\",\"content\":\"How do I reset my password?\"},{\"role\":\"assistant\",\"content\":\"To reset your password, open Settings > Security and select Reset Password.\"}]",
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Created
            };
        }

        private static FeedbackRequest ExampleFeedbackRequest()
        {
            return new FeedbackRequest
            {
                AssistantId = ApiExamples.AssistantId,
                UserMessage = "How do I reset my password?",
                AssistantResponse = "To reset your password, open Settings > Security and select Reset Password.",
                Rating = FeedbackRatingEnum.ThumbsUp,
                FeedbackText = "This was exactly what I needed!",
                MessageHistory = "[{\"role\":\"user\",\"content\":\"How do I reset my password?\"},{\"role\":\"assistant\",\"content\":\"To reset your password, open Settings > Security and select Reset Password.\"}]"
            };
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/assistants/{assistantId}/public.</summary>
        public static OpenApiRouteMetadata GetPublicInfo => ApiDoc.Create("Get public assistant information", _Tag)
            .Describe("Unauthenticated. Returns public details and chat appearance settings for an active assistant. Settings-derived fields fall back to null/false (and DocumentAttachmentMaxCount to 10) when the assistant has no settings record. Returns 404 when the assistant does not exist or is inactive.")
            .Returns(200, "Public assistant information.", new
            {
                Id = ApiExamples.AssistantId,
                Name = "Customer Support Bot",
                Description = "Answers questions about our product documentation.",
                Title = "Acme Support",
                LogoUrl = "https://www.acme.example/logo.png",
                FaviconUrl = "https://www.acme.example/favicon.ico",
                LoadModelsOnChatOpen = true,
                ExposeThinking = false,
                EnableDocumentAttachments = true,
                DocumentAttachmentMaxCount = 10,
                ExposeDocumentSourceUrls = false
            })
            .Errors(400, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/chat/open.</summary>
        public static OpenApiRouteMetadata ChatOpen => ApiDoc.Create("Notify chat window opened", _Tag)
            .Describe("Unauthenticated. No request body. When the assistant setting LoadModelsOnChatOpen is enabled, performs best-effort model-load requests for each distinct configured completion endpoint (inference, tool routing, retrieval gate, query rewrite, rerank) and the embedding endpoint, and reports per-endpoint results. "
                + "When the setting is disabled, returns Enabled=false and Loaded=false without contacting any endpoint. Endpoint identifiers and upstream response bodies are not exposed; a StatusCode of 0 indicates the load request could not be sent.")
            .Returns(200, "Model-load summary.", new
            {
                Success = true,
                Enabled = true,
                Loaded = true,
                CompletionEndpointCount = 1,
                EmbeddingEndpointCount = 1,
                Results = new[]
                {
                    new { EndpointType = "Completion", Success = true, StatusCode = 200 },
                    new { EndpointType = "Embedding", Success = true, StatusCode = 200 }
                }
            })
            .Errors(400, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/chat.</summary>
        public static OpenApiRouteMetadata Chat => ApiDoc.Create("Chat with assistant", _Tag)
            .Describe("Unauthenticated, OpenAI-compatible chat completion. Retrieves relevant chunks from the assistant's collection (RAG, with optional retrieval gate, query rewrite, and reranking), injects the assistant system prompt and context, compacts long conversations, and calls the configured inference endpoint. "
                + "The response format is controlled by the assistant's Streaming setting (the request 'stream' field is ignored): when streaming is disabled the response is a JSON chat.completion object; when enabled it is a text/event-stream of chat.completion.chunk events terminated by 'data: [DONE]'. "
                + "The final streamed chunk carries usage, retrieval, citations, and (when exposed by policy) tool_calls. Tool-enabled streaming may also emit named progress events (assistant.tool_iteration.started, assistant.tool_call.started, assistant.tool_call.heartbeat, assistant.tool_call.completed, assistant.tool_call.failed, assistant.tool_call.denied), and compaction emits a chunk with status 'Compacting the conversation...'. "
                + "Once streaming has begun, inference errors are reported in-stream as a chunk with status 'Error' rather than as an HTTP error. attached_document_ids constrains retrieval to completed documents in the assistant collection; attached_document_ids plus local_attachments must not exceed DocumentAttachmentMaxCount. "
                + "When X-Thread-ID is supplied, the turn is recorded in chat history for that thread.")
            .Header(_ThreadIdHeader, "Thread identifier from POST /v1.0/assistants/{assistantId}/threads. When provided, the turn is persisted to chat history with timing metrics.", false, ApiExamples.ThreadId)
            .Body("Chat completion request. At least one message is required; model, temperature, top_p, and max_tokens override the assistant settings.", ExampleChatRequest())
            .Returns(200, "Chat completion (JSON when the assistant's Streaming setting is disabled; server-sent events when enabled).", ExampleChatResponse())
            .ReturnsContent(200, "Server-sent event stream of chat.completion.chunk events (when the assistant's Streaming setting is enabled).", "text/event-stream", ApiSchema.String("Server-sent events; each data line is a JSON chat.completion.chunk object, a named tool progress event, or [DONE]."), _SseExample)
            .Errors(400, 404, 500)
            .Error(502, "Inference failed.", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Inference failed."));

        /// <summary>POST /v1.0/assistants/{assistantId}/feedback.</summary>
        public static OpenApiRouteMetadata SubmitFeedback => ApiDoc.Create("Submit feedback", _Tag)
            .Describe("Unauthenticated. Records a thumbs-up or thumbs-down rating (with optional text and conversation history) for an assistant response. The assistant is taken from the route; the AssistantId in the body is ignored. The tenant is inherited from the assistant.")
            .Body("Feedback to record.", ExampleFeedbackRequest())
            .Returns(201, "Feedback recorded.", ExampleFeedback())
            .Errors(400, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/compact.</summary>
        public static OpenApiRouteMetadata Compact => ApiDoc.Create("Compact conversation", _Tag)
            .Describe("Unauthenticated. Forces compaction of the supplied conversation: every message other than a leading system message and the last user message is summarized by the assistant's inference endpoint into a '[Conversation Summary]' system message. The response contains the compacted messages (the summary followed by the last user message; the configured assistant system prompt is excluded) and an estimated prompt token usage. "
                + "Clients should send the returned messages, including the summary, on the next chat request. If summarization fails the original messages are returned.")
            .Header(_ThreadIdHeader, "Thread identifier. Accepted for parity with chat but not used by compaction.", false, ApiExamples.ThreadId)
            .Body("Conversation to compact. At least one message is required; model overrides the endpoint's configured model.", ExampleCompactRequest())
            .Returns(200, "Compacted conversation.", new
            {
                messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage { Role = "system", Content = "[Conversation Summary]\nThe user asked about machine learning fundamentals and how supervised learning uses labeled data." },
                    new ChatCompletionMessage { Role = "user", Content = "How does supervised learning work?" }
                },
                usage = new ChatCompletionUsage
                {
                    PromptTokens = 250,
                    TotalTokens = 250,
                    ContextWindow = 8192
                }
            })
            .Errors(400, 404, 500);

        /// <summary>POST /v1.0/assistants/{assistantId}/generate.</summary>
        public static OpenApiRouteMetadata Generate => ApiDoc.Create("Generate completion", _Tag)
            .Describe("Unauthenticated, lightweight inference. Sends the messages as-is to the assistant's configured inference endpoint without RAG retrieval, system prompt injection, compaction, or chat history persistence. Always returns a non-streaming JSON chat.completion. Useful for auxiliary tasks such as conversation title generation.")
            .Body("Messages to send. At least one message is required; model, temperature, top_p, and max_tokens override the assistant settings.", ExampleGenerateRequest())
            .Returns(200, "Chat completion.", ExampleGenerateResponse())
            .Errors(400, 404, 500)
            .Error(502, "Inference failed.", new ApiErrorResponse(ApiErrorEnum.InternalError, null, "Inference failed."));

        /// <summary>POST /v1.0/assistants/{assistantId}/threads.</summary>
        public static OpenApiRouteMetadata CreateThread => ApiDoc.Create("Create thread", _Tag)
            .Describe("Unauthenticated. No request body. Generates a new thread identifier for an active assistant. Nothing is persisted until a chat request is sent with the identifier in the X-Thread-ID header.")
            .Returns(201, "Thread identifier created.", new { ThreadId = ApiExamples.ThreadId })
            .Errors(400, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/threads/{threadId}/history.</summary>
        public static OpenApiRouteMetadata GetThreadHistory => ApiDoc.Create("Get thread history", _Tag)
            .Describe("Unauthenticated. Returns up to 1000 chat history entries for the thread and assistant in chronological order (oldest first). Returns an empty array when the thread has no recorded turns. RetrievalContext is the JSON-serialized list of retrieved chunks.")
            .Returns(200, "Thread history entries.", new[]
            {
                new
                {
                    Id = _ChatHistoryId,
                    ThreadId = ApiExamples.ThreadId,
                    AssistantId = ApiExamples.AssistantId,
                    CollectionId = ApiExamples.CollectionId,
                    UserMessageUtc = ApiExamples.Created,
                    UserMessage = "How do I reset my password?",
                    RetrievalStartUtc = (DateTime?)ApiExamples.Created.AddMilliseconds(15),
                    RetrievalDurationMs = 42.7,
                    RetrievalGateDecision = "RETRIEVE",
                    RetrievalGateDurationMs = 210.4,
                    RetrievalContext = "[{\"document_id\":\"" + ApiExamples.DocumentId + "\",\"score\":0.87,\"content\":\"To reset your password, open Settings > Security and select Reset Password.\",\"position\":3}]",
                    PromptSentUtc = (DateTime?)ApiExamples.Created.AddMilliseconds(300),
                    PromptTokens = 412,
                    CompletionTokens = 18,
                    TokensPerSecondOverall = 14.2,
                    TokensPerSecondGeneration = 38.5,
                    TimeToFirstTokenMs = 812.3,
                    TimeToLastTokenMs = 1267.9,
                    AssistantResponse = "To reset your password, open Settings > Security and select Reset Password [1].",
                    CreatedUtc = ApiExamples.Created.AddSeconds(2)
                }
            })
            .Errors(400, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/documents.</summary>
        public static OpenApiRouteMetadata ListDocuments => ApiDoc.Create("List selectable documents", _Tag)
            .Describe("Unauthenticated. Lists completed documents in the assistant's configured collection that public chat clients can attach via attached_document_ids. Requires the assistant setting EnableDocumentAttachments (403 otherwise). Documents are also filtered by the assistant's retrieval label and tag filters. "
                + "Paging is applied before the query and contentType filters, so a page may contain fewer than maxResults items. Returns an empty page when no collection is configured. SourceUrl is populated only when ExposeDocumentSourceUrls is enabled.")
            .Paged()
            .Query("query", "string", "Case-insensitive substring match against document ID, name, original filename, content type, and (when exposed) source URL.", false, "security")
            .Query("contentType", "string", "Comma-separated MIME content type filter (up to 20 values); supports wildcards such as text/*. Alias: content_type.", false, "application/pdf,text/*")
            .Returns(200, "Page of selectable documents.", ApiExamples.Page(ExampleDocumentItem()))
            .Errors(400, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/documents/{documentId}/download.</summary>
        public static OpenApiRouteMetadata DownloadDocument => ApiDoc.Create("Download document", _Tag)
            .Describe("Unauthenticated, server-proxied download of an original document for citation links. Only available when the assistant's CitationLinkMode is Public (403 otherwise). The document must belong to the assistant's tenant. "
                + "The response Content-Type is the document's content type (application/octet-stream when unknown) and Content-Disposition is 'attachment; filename=\"<original filename>\"'. Returns 404 when the assistant, document, or stored object is missing or empty.")
            .ReturnsContent(200, "Document file contents.", "application/octet-stream", ApiSchema.Binary("Original document bytes."))
            .Errors(400, 403, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/labels/distinct.</summary>
        public static OpenApiRouteMetadata GetDistinctLabels => ApiDoc.Create("Get distinct labels", _Tag)
            .Describe("Unauthenticated. Returns the distinct label values in the assistant's configured collection, for populating metadata filter controls. Proxied to the vector store; its status code is passed through. Returns an empty array when the assistant has no settings or collection.")
            .Returns(200, "Distinct label values.", new List<string> { "support", "billing", "security" })
            .Errors(400, 404, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}/tags/distinct.</summary>
        public static OpenApiRouteMetadata GetDistinctTags => ApiDoc.Create("Get distinct tag keys", _Tag)
            .Describe("Unauthenticated. Returns the distinct tag keys in the assistant's configured collection, for populating metadata filter controls. Proxied to the vector store; its status code is passed through. Returns an empty array when the assistant has no settings or collection.")
            .Returns(200, "Distinct tag keys.", new List<string> { "department", "year", "status" })
            .Errors(400, 404, 500);

        #endregion
    }
}
