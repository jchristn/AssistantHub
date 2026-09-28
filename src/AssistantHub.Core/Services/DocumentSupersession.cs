namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Maintains supersession links between documents: a replacing document lists the documents it supersedes, and
    /// each superseded document records its replacement, which retrieval uses to demote, hide or mark it.
    /// </summary>
    public static class DocumentSupersession
    {
        #region Public-Methods

        /// <summary>
        /// Parse a JSON array of document identifiers, tolerating empty or invalid values.
        /// </summary>
        /// <param name="json">JSON array.</param>
        /// <returns>Identifiers.</returns>
        public static List<string> ParseIds(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json)?
                    .Where(id => !String.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToList()
                    ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Check that a document can supersede the given documents: each exists in the same tenant, and none is the
        /// document itself or its own replacement.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="replacement">Replacing document (its Id may not be persisted yet).</param>
        /// <param name="targetIds">Documents to supersede.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>An error message, or null when valid.</returns>
        public static async Task<string> ValidateAsync(DatabaseDriverBase database, AssistantDocument replacement, IEnumerable<string> targetIds, CancellationToken token = default)
        {
            foreach (string id in targetIds ?? Enumerable.Empty<string>())
            {
                if (String.Equals(id, replacement.Id, StringComparison.Ordinal))
                    return "A document cannot supersede itself.";
                if (String.Equals(id, replacement.SupersededBy, StringComparison.Ordinal))
                    return "Document " + id + " supersedes this document, so this document cannot supersede it.";

                AssistantDocument target = await database.AssistantDocument.ReadAsync(id, token).ConfigureAwait(false);
                if (target == null || !String.Equals(target.TenantId, replacement.TenantId, StringComparison.Ordinal))
                    return "Superseded document not found: " + id;
            }

            return null;
        }

        /// <summary>
        /// Set the documents a replacing document supersedes, releasing any it no longer supersedes.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="replacement">Replacing document.</param>
        /// <param name="targetIds">Documents it supersedes (empty to clear).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public static async Task SetAsync(DatabaseDriverBase database, AssistantDocument replacement, List<string> targetIds, CancellationToken token = default)
        {
            targetIds = (targetIds ?? new List<string>()).Where(id => !String.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();

            foreach (AssistantDocument previous in await database.AssistantDocument.ReadSupersededByAsync(replacement.TenantId, replacement.Id, token).ConfigureAwait(false))
            {
                if (!targetIds.Contains(previous.Id, StringComparer.Ordinal))
                    await database.AssistantDocument.UpdateSupersessionAsync(previous.Id, previous.Supersedes, null, token).ConfigureAwait(false);
            }

            foreach (string id in targetIds)
            {
                AssistantDocument target = await database.AssistantDocument.ReadAsync(id, token).ConfigureAwait(false);
                if (target != null)
                    await database.AssistantDocument.UpdateSupersessionAsync(target.Id, target.Supersedes, replacement.Id, token).ConfigureAwait(false);
            }

            string json = targetIds.Count > 0 ? JsonSerializer.Serialize(targetIds) : null;
            await database.AssistantDocument.UpdateSupersessionAsync(replacement.Id, json, replacement.SupersededBy, token).ConfigureAwait(false);
            replacement.Supersedes = json;
        }

        /// <summary>
        /// Release a document's supersession links before it is deleted: the documents it superseded become current
        /// again, and its own replacement stops listing it.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="document">Document being deleted.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public static async Task ReleaseAsync(DatabaseDriverBase database, AssistantDocument document, CancellationToken token = default)
        {
            if (document == null || String.IsNullOrEmpty(document.TenantId)) return;

            foreach (AssistantDocument superseded in await database.AssistantDocument.ReadSupersededByAsync(document.TenantId, document.Id, token).ConfigureAwait(false))
                await database.AssistantDocument.UpdateSupersessionAsync(superseded.Id, superseded.Supersedes, null, token).ConfigureAwait(false);

            if (!String.IsNullOrEmpty(document.SupersededBy))
            {
                AssistantDocument replacement = await database.AssistantDocument.ReadAsync(document.SupersededBy, token).ConfigureAwait(false);
                if (replacement != null)
                {
                    List<string> remaining = ParseIds(replacement.Supersedes).Where(id => !String.Equals(id, document.Id, StringComparison.Ordinal)).ToList();
                    await database.AssistantDocument.UpdateSupersessionAsync(
                        replacement.Id, remaining.Count > 0 ? JsonSerializer.Serialize(remaining) : null, replacement.SupersededBy, token).ConfigureAwait(false);
                }
            }
        }

        #endregion
    }
}
