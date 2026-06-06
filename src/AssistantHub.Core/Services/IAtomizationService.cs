namespace AssistantHub.Core.Services
{
    using System.Threading;
    using System.Threading.Tasks;

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
    }
}
