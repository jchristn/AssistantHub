namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Resolves and validates attached document identifiers for assistant chat.
    /// </summary>
    public class AssistantDocumentAttachmentResolver
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Database;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        public AssistantDocumentAttachmentResolver(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Resolve attached documents and enforce assistant tenant, collection, status, and count policy.
        /// </summary>
        /// <param name="assistant">Assistant.</param>
        /// <param name="settings">Assistant settings.</param>
        /// <param name="documentIds">Client-supplied document identifiers.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Resolution result.</returns>
        public async Task<AssistantDocumentAttachmentResolution> ResolveAsync(
            Assistant assistant,
            AssistantSettings settings,
            IEnumerable<string> documentIds,
            CancellationToken token = default)
        {
            AssistantDocumentAttachmentResolution result = new AssistantDocumentAttachmentResolution();

            List<string> normalized = Normalize(documentIds);
            if (normalized.Count < 1)
            {
                result.Success = true;
                return result;
            }

            if (assistant == null)
                return Error(404, "Assistant not found.");

            if (settings == null)
                return Error(500, "Assistant settings not configured.");

            if (!settings.EnableDocumentAttachments)
                return Error(400, "Document attachments are not enabled for this assistant.");

            if (String.IsNullOrWhiteSpace(settings.CollectionId))
                return Error(400, "Assistant collection is not configured.");

            if (normalized.Count > settings.DocumentAttachmentMaxCount)
                return Error(400, "Too many documents attached. Maximum allowed is " + settings.DocumentAttachmentMaxCount + ".");

            foreach (string documentId in normalized)
            {
                AssistantDocument document = await _Database.AssistantDocument.ReadAsync(documentId, token).ConfigureAwait(false);
                if (!IsAvailable(document, assistant, settings))
                    return Error(400, "Attached document is not available for this assistant: " + documentId + ".");

                result.DocumentIds.Add(document.Id);
                result.Documents.Add(AssistantDocumentSelectionItem.FromDocument(document, settings.ExposeDocumentSourceUrls));
            }

            result.Success = true;
            return result;
        }

        #endregion

        #region Private-Methods

        private static List<string> Normalize(IEnumerable<string> documentIds)
        {
            if (documentIds == null) return new List<string>();

            return documentIds
                .Where(id => !String.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsAvailable(AssistantDocument document, Assistant assistant, AssistantSettings settings)
        {
            if (document == null || assistant == null || settings == null) return false;
            if (!String.Equals(document.TenantId, assistant.TenantId, StringComparison.Ordinal)) return false;
            if (!String.Equals(document.CollectionId, settings.CollectionId, StringComparison.Ordinal)) return false;
            if (document.Status != DocumentStatusEnum.Completed) return false;
            if (!AssistantDocumentPolicyFilter.MatchesAssistantMetadataFilters(document, settings)) return false;
            return true;
        }

        private static AssistantDocumentAttachmentResolution Error(int statusCode, string message)
        {
            return new AssistantDocumentAttachmentResolution
            {
                Success = false,
                StatusCode = statusCode,
                ErrorMessage = message
            };
        }

        #endregion
    }
}
