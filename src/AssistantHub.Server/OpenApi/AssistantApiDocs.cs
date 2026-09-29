namespace AssistantHub.Server.OpenApi
{
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for assistant CRUD routes.
    /// </summary>
    public static class AssistantApiDocs
    {
        #region Private-Members

        private const string _Tag = "Assistants";

        private static Assistant ExampleAssistant()
        {
            return new Assistant
            {
                Id = ApiExamples.AssistantId,
                TenantId = ApiExamples.TenantId,
                UserId = ApiExamples.UserId,
                Name = "Support Assistant",
                Description = "Answers customer questions from the support knowledge base.",
                Active = true,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Updated
            };
        }

        private static Assistant ExampleAssistantRequest()
        {
            return new Assistant
            {
                Id = ApiExamples.AssistantId,
                TenantId = ApiExamples.TenantId,
                UserId = ApiExamples.UserId,
                Name = "Support Assistant",
                Description = "Answers customer questions from the support knowledge base.",
                Active = true,
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Created
            };
        }

        #endregion

        #region Public-Members

        /// <summary>PUT /v1.0/assistants.</summary>
        public static OpenApiRouteMetadata Create => ApiDoc.Create("Create assistant", _Tag)
            .Describe("Creates an assistant owned by the calling user in the caller's tenant. Name is required. Id, TenantId, UserId, CreatedUtc, and LastUpdateUtc are assigned by the server and any supplied values are ignored, except with the administrator API key, which belongs to no tenant or user: TenantId is then required in the body (400 when missing, 404 when the tenant does not exist), and the assistant is owned by the body's UserId (400 unless it is a user of that tenant) or else the tenant's first administrator. "
                + "Default assistant settings are created alongside the assistant: RAG is enabled against the collection of the first ingestion rule in the tenant (when one exists), and the first available completion and embedding endpoints are assigned when they can be enumerated. Any authenticated user may create an assistant.")
            .Body("Assistant to create. Name is required; Description and Active are optional.", ExampleAssistantRequest())
            .Returns(201, "Assistant created.", ExampleAssistant())
            .Errors(400, 401, 404, 500);

        /// <summary>GET /v1.0/assistants.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List assistants", _Tag)
            .Describe("Enumerates assistants in the caller's tenant. Global and tenant administrators see every assistant in the tenant; other users see only the assistants they own (the filter is applied to the returned page).")
            .Paged()
            .Returns(200, "Page of assistants.", ApiExamples.Page(ExampleAssistant()))
            .Errors(401, 500);

        /// <summary>GET /v1.0/assistants/{assistantId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get assistant", _Tag)
            .Describe("Returns one assistant. Assistants in another tenant return 404 unless the caller is a global administrator. Non-administrators can only read assistants they own (403 otherwise).")
            .Returns(200, "Assistant.", ExampleAssistant())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>PUT /v1.0/assistants/{assistantId}.</summary>
        public static OpenApiRouteMetadata Update => ApiDoc.Create("Update assistant", _Tag)
            .Describe("Replaces the mutable fields of an assistant (Name, Description, Active). Id, TenantId, UserId, and CreatedUtc are preserved from the stored record and LastUpdateUtc is set by the server. Owner or administrator only.")
            .Body("Updated assistant values.", ExampleAssistantRequest())
            .Returns(200, "Updated assistant.", ExampleAssistant())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/assistants/{assistantId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete assistant", _Tag)
            .Describe("Deletes an assistant together with its settings and feedback records. Owner or administrator only.")
            .ReturnsNoContent(204, "Assistant deleted.")
            .Errors(400, 401, 403, 404, 500);

        /// <summary>HEAD /v1.0/assistants/{assistantId}.</summary>
        public static OpenApiRouteMetadata Exists => ApiDoc.Create("Check assistant existence", _Tag)
            .Describe("Returns 200 when the assistant exists and the caller can access it (owner or administrator), 404 when it does not exist or belongs to another tenant, and 403 when a non-administrator does not own it. No response body.")
            .ReturnsNoContent(200, "Assistant exists.")
            .ReturnsNoContent(400, "Bad request.")
            .ReturnsNoContent(401, "Authentication failed.")
            .ReturnsNoContent(403, "Authorization failed.")
            .ReturnsNoContent(404, "Assistant not found.")
            .ReturnsNoContent(500, "Internal server error.");

        #endregion
    }
}
