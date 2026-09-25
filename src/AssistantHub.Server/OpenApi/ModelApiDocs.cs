namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections.Generic;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Server.Handlers;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for inference model routes.
    /// </summary>
    public static class ModelApiDocs
    {
        #region Private-Members

        private const string _Tag = "Models";

        private const string _AssistantIdDescription = "Target the inference endpoint configured in this assistant's settings instead of the server's default inference provider. Returns 404 when the assistant has no settings.";

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/models.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List models", _Tag)
            .Describe("Lists the models available on the server's configured inference provider, or, with assistantId, on the completion endpoint referenced by that assistant's settings (its provider, URL, API key, and default model are resolved from Partio). Available to any authenticated user. "
                + "SizeBytes is 0 for hosted providers that do not report it; PullSupported indicates whether the provider supports pulling models (Ollama).")
            .Query("assistantId", "string", _AssistantIdDescription, false, ApiExamples.AssistantId)
            .Returns(200, "Available models.", new List<InferenceModel>
            {
                new InferenceModel
                {
                    Name = "gemma3:4b",
                    SizeBytes = 3338801804,
                    ModifiedUtc = ApiExamples.Created,
                    OwnedBy = null,
                    PullSupported = true
                },
                new InferenceModel
                {
                    Name = "all-minilm:latest",
                    SizeBytes = 45960996,
                    ModifiedUtc = ApiExamples.Updated,
                    OwnedBy = null,
                    PullSupported = true
                }
            })
            .Errors(401, 404, 500);

        /// <summary>POST /v1.0/models/pull.</summary>
        public static OpenApiRouteMetadata Pull => ApiDoc.Create("Pull model", _Tag)
            .Describe("Global administrators only. Starts downloading a model on the inference provider (Ollama only) in the background and returns 202 immediately; poll GET /v1.0/models/pull/status for progress. "
                + "With assistantId, the pull targets the completion endpoint referenced by that assistant's settings. Only one pull runs at a time; a second request while one is in progress returns 409. "
                + "Returns 400 when the body is empty, Name is missing, or the provider does not support pulling.")
            .Query("assistantId", "string", _AssistantIdDescription, false, ApiExamples.AssistantId)
            .Body("Model to pull.", new PullModelRequest { Name = "gemma3:4b" })
            .Returns(202, "Pull started.", new { ModelName = "gemma3:4b", Status = "starting" })
            .Errors(400, 401, 403, 404, 500)
            .Error(409, "A pull operation is already in progress.", new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "A pull operation is already in progress."));

        /// <summary>GET /v1.0/models/pull/status.</summary>
        public static OpenApiRouteMetadata PullStatus => ApiDoc.Create("Get model pull status", _Tag)
            .Describe("Global administrators only. Returns progress of the most recent pull started on this server instance, including completed ones (check IsComplete and HasError). Returns 404 when no pull has been started since the server started.")
            .Returns(200, "Pull progress.", new PullProgress
            {
                ModelName = "gemma3:4b",
                Status = "pulling aa8bb2c7a4e3",
                Digest = "sha256:aa8bb2c7a4e3b6f3f8f1e8a0b8d1c2e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1",
                TotalBytes = 3338801804,
                CompletedBytes = 1201968649,
                PercentComplete = 36,
                IsComplete = false,
                HasError = false,
                ErrorMessage = null,
                StartedUtc = ApiExamples.Updated
            })
            .Error(404, "No pull operation has been started.", new ApiErrorResponse(ApiErrorEnum.NotFound, null, "No pull operation in progress."))
            .Errors(401, 403, 500);

        /// <summary>DELETE /v1.0/models/{modelName}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete model", _Tag)
            .Describe("Global administrators only. Removes a model from the inference provider (Ollama only). The model name in the path is URL-decoded, so encode names containing ':' or '/' (for example gemma3%3A4b). "
                + "With assistantId, the delete targets the completion endpoint referenced by that assistant's settings. Returns 404 when the provider reports that the model could not be deleted.")
            .Query("assistantId", "string", _AssistantIdDescription, false, ApiExamples.AssistantId)
            .ReturnsNoContent(204, "Model deleted.")
            .Errors(400, 401, 403, 404, 500);

        #endregion
    }
}
