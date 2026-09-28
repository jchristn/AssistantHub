namespace AssistantHub.Core.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Models;

    /// <summary>
    /// Document atomization and type-detection service abstraction.
    /// </summary>
    public interface IAtomizationService
    {
        /// <summary>
        /// Detect the type of a document.
        /// </summary>
        Task<TypeDetectResponse> DetectDocumentTypeAsync(string documentId, byte[] fileBytes, string filename, CancellationToken token = default);

        /// <summary>
        /// Extract text content from a document.
        /// </summary>
        Task<string> ExtractTextAsync(string documentId, byte[] fileBytes, string documentType, string filename, CancellationToken token = default);

        /// <summary>
        /// Extract a document's text and its structural blocks, applying an ingestion rule's extraction settings.
        /// Implementations that only produce text return it with no blocks.
        /// </summary>
        async Task<AtomExtractionResult> ExtractAsync(string documentId, byte[] fileBytes, string documentType, string filename, IngestionExtractionConfig extraction, CancellationToken token = default)
        {
            string text = await ExtractTextAsync(documentId, fileBytes, documentType, filename, token).ConfigureAwait(false);
            return text == null ? null : new AtomExtractionResult { Text = text };
        }
    }
}
