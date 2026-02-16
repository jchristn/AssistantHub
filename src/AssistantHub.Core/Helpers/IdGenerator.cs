namespace AssistantHub.Core.Helpers
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Helper class for generating K-sortable identifiers and bearer tokens.
    /// </summary>
    public static class IdGenerator
    {
        #region Private-Members

        private static PrettyId.IdGenerator _Generator = new PrettyId.IdGenerator();

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generate a user identifier.
        /// </summary>
        /// <returns>User identifier.</returns>
        public static string NewUserId()
        {
            return _Generator.GenerateKSortable(Constants.UserIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate a credential identifier.
        /// </summary>
        /// <returns>Credential identifier.</returns>
        public static string NewCredentialId()
        {
            return _Generator.GenerateKSortable(Constants.CredentialIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate an assistant identifier.
        /// </summary>
        /// <returns>Assistant identifier.</returns>
        public static string NewAssistantId()
        {
            return _Generator.GenerateKSortable(Constants.AssistantIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate an assistant settings identifier.
        /// </summary>
        /// <returns>Assistant settings identifier.</returns>
        public static string NewAssistantSettingsId()
        {
            return _Generator.GenerateKSortable(Constants.AssistantSettingsIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate an assistant document identifier.
        /// </summary>
        /// <returns>Assistant document identifier.</returns>
        public static string NewAssistantDocumentId()
        {
            return _Generator.GenerateKSortable(Constants.AssistantDocumentIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate an assistant feedback identifier.
        /// </summary>
        /// <returns>Assistant feedback identifier.</returns>
        public static string NewAssistantFeedbackId()
        {
            return _Generator.GenerateKSortable(Constants.AssistantFeedbackIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate an ingestion rule identifier.
        /// </summary>
        /// <returns>Ingestion rule identifier.</returns>
        public static string NewIngestionRuleId()
        {
            return _Generator.GenerateKSortable(Constants.IngestionRuleIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate a thread identifier.
        /// </summary>
        /// <returns>Thread identifier.</returns>
        public static string NewThreadId()
        {
            return _Generator.GenerateKSortable(Constants.ThreadIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate a chat history identifier.
        /// </summary>
        /// <returns>Chat history identifier.</returns>
        public static string NewChatHistoryId()
        {
            return _Generator.GenerateKSortable(Constants.ChatHistoryIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate a chat completion identifier.
        /// </summary>
        /// <returns>Chat completion identifier.</returns>
        public static string NewChatCompletionId()
        {
            return _Generator.GenerateKSortable(Constants.ChatCompletionIdentifierPrefix, Constants.IdentifierLength);
        }

        /// <summary>
        /// Generate a cryptographic random bearer token.
        /// </summary>
        /// <returns>Bearer token string.</returns>
        public static string NewBearerToken()
        {
            byte[] bytes = new byte[Constants.BearerTokenLength / 2];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        #endregion
    }
}
