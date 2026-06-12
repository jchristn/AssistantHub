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
                foreach (AtomResponse atom in atoms)
                    AppendReadableContent(atom, sb);

                if (_ProcessingLog != null)
                    await _ProcessingLog.LogAsync(documentId, "INFO", "Readable content extracted: " + sb.Length + " character(s)").ConfigureAwait(false);

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

        private static void AppendReadableContent(AtomResponse atom, StringBuilder sb)
        {
            if (atom == null) return;

            string content = RenderAtomContent(atom);
            if (!String.IsNullOrWhiteSpace(content))
            {
                AppendBlock(sb, content);
            }
            else if (atom.Chunks != null)
            {
                foreach (AtomChunkResponse chunk in atom.Chunks)
                {
                    if (!String.IsNullOrWhiteSpace(chunk?.Text))
                        AppendBlock(sb, chunk.Text);
                }
            }

            if (atom.Quarks != null)
            {
                foreach (AtomResponse quark in atom.Quarks)
                    AppendReadableContent(quark, sb);
            }
        }

        private static string RenderAtomContent(AtomResponse atom)
        {
            if (atom == null) return null;

            if (!String.IsNullOrWhiteSpace(atom.Text))
            {
                if (IsAtomType(atom, "Hyperlink") && !String.IsNullOrWhiteSpace(atom.Title))
                    return "[" + atom.Title.Trim() + "](" + atom.Text.Trim() + ")";

                if (IsAtomType(atom, "Code"))
                    return "```" + Environment.NewLine + atom.Text + Environment.NewLine + "```";

                return atom.Text;
            }

            string list = RenderList(atom.OrderedList, true);
            if (!String.IsNullOrWhiteSpace(list)) return list;

            list = RenderList(atom.UnorderedList, false);
            if (!String.IsNullOrWhiteSpace(list)) return list;

            string table = RenderTable(atom.Table);
            if (!String.IsNullOrWhiteSpace(table)) return table;

            if (!String.IsNullOrWhiteSpace(atom.Title) && !String.IsNullOrWhiteSpace(atom.Subtitle))
                return atom.Title.Trim() + Environment.NewLine + atom.Subtitle.Trim();

            if (!String.IsNullOrWhiteSpace(atom.Title)) return atom.Title.Trim();
            if (!String.IsNullOrWhiteSpace(atom.Subtitle)) return atom.Subtitle.Trim();

            return null;
        }

        private static string RenderList(List<string> items, bool ordered)
        {
            if (items == null || items.Count < 1) return null;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                string item = items[i];
                if (String.IsNullOrWhiteSpace(item)) continue;

                if (ordered) sb.Append((i + 1).ToString() + ". ");
                else sb.Append("- ");

                sb.AppendLine(item.Trim());
            }

            return sb.ToString().TrimEnd();
        }

        private static string RenderTable(JsonElement table)
        {
            if (table.ValueKind == JsonValueKind.Undefined || table.ValueKind == JsonValueKind.Null)
                return null;

            if (!TryGetProperty(table, "Columns", out JsonElement columnsElement)
                || columnsElement.ValueKind != JsonValueKind.Array)
                columnsElement = default;

            if (!TryGetProperty(table, "Rows", out JsonElement rowsElement)
                || rowsElement.ValueKind != JsonValueKind.Array)
                rowsElement = default;

            List<string> columns = ExtractColumnNames(columnsElement);
            List<List<string>> rows = ExtractRows(rowsElement, columns);

            if (columns.Count < 1 && rows.Count > 0)
            {
                int width = 0;
                foreach (List<string> row in rows)
                {
                    if (row.Count > width) width = row.Count;
                }

                for (int i = 0; i < width; i++)
                    columns.Add("Column" + (i + 1).ToString());
            }

            if (columns.Count < 1 && rows.Count < 1)
                return null;

            StringBuilder sb = new StringBuilder();
            if (columns.Count > 0)
            {
                sb.Append("| ");
                sb.Append(String.Join(" | ", columns.ConvertAll(EscapeTableCell)));
                sb.AppendLine(" |");
                sb.Append("| ");
                sb.Append(String.Join(" | ", columns.ConvertAll(_ => "---")));
                sb.AppendLine(" |");
            }

            foreach (List<string> row in rows)
            {
                while (row.Count < columns.Count)
                    row.Add(String.Empty);

                sb.Append("| ");
                sb.Append(String.Join(" | ", row.ConvertAll(EscapeTableCell)));
                sb.AppendLine(" |");
            }

            return sb.ToString().TrimEnd();
        }

        private static List<string> ExtractColumnNames(JsonElement columnsElement)
        {
            List<string> columns = new List<string>();
            if (columnsElement.ValueKind != JsonValueKind.Array) return columns;

            foreach (JsonElement column in columnsElement.EnumerateArray())
            {
                string name = null;
                if (column.ValueKind == JsonValueKind.String)
                    name = column.GetString();
                else if (column.ValueKind == JsonValueKind.Object
                    && TryGetProperty(column, "Name", out JsonElement nameElement))
                    name = GetScalarText(nameElement);
                else
                    name = GetScalarText(column);

                if (String.IsNullOrWhiteSpace(name))
                    name = "Column" + (columns.Count + 1).ToString();

                columns.Add(name);
            }

            return columns;
        }

        private static List<List<string>> ExtractRows(JsonElement rowsElement, List<string> columns)
        {
            List<List<string>> rows = new List<List<string>>();
            if (rowsElement.ValueKind != JsonValueKind.Array) return rows;

            List<string> effectiveColumns = columns;
            foreach (JsonElement rowElement in rowsElement.EnumerateArray())
            {
                List<string> row = new List<string>();

                if (rowElement.ValueKind == JsonValueKind.Object)
                {
                    if (effectiveColumns.Count < 1)
                    {
                        foreach (JsonProperty prop in rowElement.EnumerateObject())
                            effectiveColumns.Add(prop.Name);
                    }

                    foreach (string column in effectiveColumns)
                    {
                        if (TryGetProperty(rowElement, column, out JsonElement valueElement))
                            row.Add(GetScalarText(valueElement) ?? String.Empty);
                        else
                            row.Add(String.Empty);
                    }
                }
                else if (rowElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement valueElement in rowElement.EnumerateArray())
                        row.Add(GetScalarText(valueElement) ?? String.Empty);
                }
                else
                {
                    row.Add(GetScalarText(rowElement) ?? String.Empty);
                }

                rows.Add(row);
            }

            return rows;
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
        {
            value = default;
            if (element.ValueKind != JsonValueKind.Object) return false;

            foreach (JsonProperty prop in element.EnumerateObject())
            {
                if (String.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }

            return false;
        }

        private static string GetScalarText(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.ToString();
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return element.ToString();
            }
        }

        private static bool IsAtomType(AtomResponse atom, string type)
        {
            string atomType = GetAtomType(atom);
            return String.Equals(atomType, type, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetAtomType(AtomResponse atom)
        {
            if (atom == null) return null;

            switch (atom.Type.ValueKind)
            {
                case JsonValueKind.String:
                    return atom.Type.GetString();
                case JsonValueKind.Number:
                    if (!atom.Type.TryGetInt32(out int numericType)) return null;
                    switch (numericType)
                    {
                        case 0: return "Text";
                        case 1: return "List";
                        case 2: return "Binary";
                        case 3: return "Table";
                        case 4: return "Unknown";
                        case 5: return "Image";
                        case 6: return "Hyperlink";
                        case 7: return "Code";
                        case 8: return "Meta";
                        default: return null;
                    }
                default:
                    return null;
            }
        }

        private static string EscapeTableCell(string value)
        {
            if (value == null) return String.Empty;
            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("|", "\\|")
                .Trim();
        }

        private static void AppendBlock(StringBuilder sb, string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return;
            if (sb.Length > 0) sb.Append(Environment.NewLine);
            sb.Append(text.Trim());
        }

        #endregion
    }
}
