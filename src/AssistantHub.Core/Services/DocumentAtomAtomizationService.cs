namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// DocumentAtom implementation of the atomization service.
    /// </summary>
    public class DocumentAtomAtomizationService : IAtomizationService
    {
        #region Private-Members

        private const string _Header = "[DocumentAtomAtomizationService] ";
        private static readonly HttpClient _HttpClient = new HttpClient();
        private readonly DocumentAtomSettings _Settings;
        private readonly LoggingModule _Logging;
        private readonly ProcessingLogService _ProcessingLog;
        private readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">DocumentAtom settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="processingLog">Optional processing log service.</param>
        public DocumentAtomAtomizationService(DocumentAtomSettings settings, LoggingModule logging, ProcessingLogService processingLog = null)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _ProcessingLog = processingLog;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<TypeDetectResponse> DetectDocumentTypeAsync(string documentId, byte[] fileBytes, string filename, CancellationToken token = default)
        {
            string url = _Settings.Endpoint.TrimEnd('/') + "/typedetect";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new ByteArrayContent(fileBytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                if (!String.IsNullOrEmpty(_Settings.AccessKey))
                    request.Headers.Add("x-api-key", _Settings.AccessKey);

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "type detection returned " + (int)response.StatusCode + ": " + responseBody);
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Type detection API returned HTTP " + (int)response.StatusCode + ": " + responseBody).ConfigureAwait(false);
                    return null;
                }

                TypeDetectResponse typeResult = JsonSerializer.Deserialize<TypeDetectResponse>(responseBody, _JsonOptions);

                _Logging.Debug(_Header + "type detection response for document " + documentId + ": " + typeResult?.MimeType);
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "DEBUG", "Type detection response: " + typeResult?.MimeType).ConfigureAwait(false);

                return typeResult;
            }
        }

        /// <inheritdoc />
        public async Task<string> ExtractTextAsync(string documentId, byte[] fileBytes, string documentType, string filename, CancellationToken token = default)
        {
            string atomPath = GetAtomPath(documentType);
            if (String.IsNullOrEmpty(atomPath))
            {
                _Logging.Warn(_Header + "no atom endpoint for document type: " + documentType);
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "ERROR", "No atom endpoint for document type: " + documentType).ConfigureAwait(false);
                return null;
            }

            string url = _Settings.Endpoint.TrimEnd('/') + atomPath;

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                object atomRequest = new
                {
                    Settings = (object)null,
                    Data = Convert.ToBase64String(fileBytes)
                };

                string requestJson = JsonSerializer.Serialize(atomRequest, _JsonOptions);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                if (!String.IsNullOrEmpty(_Settings.AccessKey))
                    request.Headers.Add("x-api-key", _Settings.AccessKey);

                HttpResponseMessage response = await _HttpClient.SendAsync(request, token).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _Logging.Warn(_Header + "document processing returned " + (int)response.StatusCode + ": " + responseBody);
                    if (_ProcessingLog != null)
                        await _ProcessingLog.LogAsync(documentId, "ERROR", "Atom extraction API returned HTTP " + (int)response.StatusCode + ": " + responseBody).ConfigureAwait(false);
                    return null;
                }

                _Logging.Debug(_Header + "atom extraction response for document " + documentId + ": " + responseBody.Length + " characters");
                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "DEBUG", "Atom extraction response: " + responseBody.Length + " characters").ConfigureAwait(false);

                List<AtomResponse> atoms = JsonSerializer.Deserialize<List<AtomResponse>>(responseBody, _JsonOptions);
                if (atoms == null || atoms.Count == 0)
                    return null;

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Atoms extracted: " + atoms.Count + " atom(s)").ConfigureAwait(false);

                StringBuilder sb = new StringBuilder();
                int atomIndex = 0;
                foreach (AtomResponse atom in atoms)
                {
                    atomIndex++;
                    if (!String.IsNullOrEmpty(atom.Text))
                    {
                        if (sb.Length > 0) sb.Append(Environment.NewLine);
                        sb.Append(atom.Text);
                        if (_ProcessingLog != null)
                            await _ProcessingLog.LogAsync(documentId, "DEBUG", "Atom [" + atomIndex + "/" + atoms.Count + "] - " + atom.Text.Length + " characters").ConfigureAwait(false);
                    }
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }
        }

        #endregion

        #region Private-Methods

        private static string GetAtomPath(string documentType)
        {
            if (String.IsNullOrEmpty(documentType)) return null;

            switch (documentType.ToLowerInvariant())
            {
                case "csv": return "/atom/csv";
                case "xlsx":
                case "xls": return "/atom/excel";
                case "html": return "/atom/html";
                case "json": return "/atom/json";
                case "markdown": return "/atom/markdown";
                case "pdf": return "/atom/pdf";
                case "png":
                case "jpeg":
                case "gif":
                case "tiff":
                case "bmp":
                case "webp":
                case "ico": return "/atom/png";
                case "pptx":
                case "ppt": return "/atom/powerpoint";
                case "rtf": return "/atom/rtf";
                case "text":
                case "tsv": return "/atom/text";
                case "docx":
                case "doc": return "/atom/word";
                case "xml":
                case "svg":
                case "gpx": return "/atom/xml";
                default: return null;
            }
        }

        #endregion
    }
}
