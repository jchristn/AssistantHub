namespace AssistantHub.Server.OpenApi
{
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// OpenAPI metadata for assistant feedback routes.
    /// </summary>
    public static class FeedbackApiDocs
    {
        #region Private-Members

        private const string _Tag = "Feedback";

        private const string _FeedbackId = "afb_01JH4A2B3C4D5E6F7G8H9J0K1M";

        private static AssistantFeedback ExampleFeedback()
        {
            return new AssistantFeedback
            {
                Id = _FeedbackId,
                TenantId = ApiExamples.TenantId,
                AssistantId = ApiExamples.AssistantId,
                UserMessage = "What is your return policy?",
                AssistantResponse = "Our return policy allows returns within 30 days of purchase for a full refund with the original receipt.",
                Rating = FeedbackRatingEnum.ThumbsUp,
                FeedbackText = "Very helpful answer!",
                MessageHistory = "[{\"role\":\"user\",\"content\":\"What is your return policy?\"},{\"role\":\"assistant\",\"content\":\"Our return policy allows returns within 30 days of purchase for a full refund with the original receipt.\"}]",
                CreatedUtc = ApiExamples.Created,
                LastUpdateUtc = ApiExamples.Created
            };
        }

        #endregion

        #region Public-Members

        /// <summary>GET /v1.0/feedback.</summary>
        public static OpenApiRouteMetadata List => ApiDoc.Create("List feedback", _Tag)
            .Describe("Enumerates thumbs-up/thumbs-down feedback records in the caller's tenant. Global and tenant administrators see all feedback in the tenant; other users only see feedback for assistants they own (filtering is applied to the returned page, so a page may contain fewer than maxResults records).")
            .Paged()
            .Query("assistantId", "string", "Only return feedback for this assistant.", false, ApiExamples.AssistantId)
            .Returns(200, "Page of feedback records.", ApiExamples.Page(ExampleFeedback()))
            .Errors(401, 500);

        /// <summary>GET /v1.0/feedback/{feedbackId}.</summary>
        public static OpenApiRouteMetadata Read => ApiDoc.Create("Get feedback", _Tag)
            .Describe("Returns one feedback record. The record must belong to the caller's tenant (global administrators can read any tenant). Non-administrators must own the assistant the feedback belongs to.")
            .Returns(200, "Feedback record.", ExampleFeedback())
            .Errors(400, 401, 403, 404, 500);

        /// <summary>DELETE /v1.0/feedback/{feedbackId}.</summary>
        public static OpenApiRouteMetadata Delete => ApiDoc.Create("Delete feedback", _Tag)
            .Describe("Permanently deletes one feedback record. The record must belong to the caller's tenant (global administrators can delete any tenant's feedback). Non-administrators must own the assistant the feedback belongs to.")
            .ReturnsNoContent(204, "Feedback deleted.")
            .Errors(400, 401, 403, 404, 500);

        #endregion
    }
}
