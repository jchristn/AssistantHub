namespace Test.Benchmark.Datasets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Loads and saves datasets and reports.
    /// </summary>
    public static class DatasetStore
    {
        #region Public-Members

        /// <summary>
        /// JSON options shared by datasets and reports: camelCase, case-insensitive reads, nulls omitted.
        /// </summary>
        public static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Load a dataset file.
        /// </summary>
        /// <param name="path">Path to the dataset JSON.</param>
        /// <returns>The dataset.</returns>
        public static BenchmarkDataset Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Dataset not found: " + path);
            byte[] bytes = File.ReadAllBytes(path);
            BenchmarkDataset? dataset = JsonSerializer.Deserialize<BenchmarkDataset>(bytes, Json);
            if (dataset == null || dataset.Corpora.Count == 0) throw new InvalidDataException("Dataset '" + path + "' has no corpora.");
            if (string.IsNullOrEmpty(dataset.Name)) dataset.Name = Path.GetFileNameWithoutExtension(path);

            dataset.BaseDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
            dataset.FileHash = Sha256(bytes).Substring(0, 16);
            foreach (BenchmarkCorpus corpus in dataset.Corpora)
            {
                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (BenchmarkDocument document in corpus.Documents)
                {
                    if (!ids.Add(document.Id)) throw new InvalidDataException("Duplicate document id '" + document.Id + "' in corpus '" + corpus.Id + "'.");
                }
            }

            return dataset;
        }

        /// <summary>
        /// Save a dataset.
        /// </summary>
        /// <param name="dataset">Dataset.</param>
        /// <param name="path">Output path.</param>
        public static void Save(BenchmarkDataset dataset, string path)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonSerializer.Serialize(dataset, Json), new UTF8Encoding(false));
        }

        /// <summary>
        /// Read a document's content bytes and resolve its content type.
        /// </summary>
        /// <param name="dataset">Dataset (for the base directory).</param>
        /// <param name="document">Document.</param>
        /// <param name="contentType">Resolved content type.</param>
        /// <param name="fileName">File name to upload as.</param>
        /// <returns>Content bytes.</returns>
        public static byte[] ReadContent(BenchmarkDataset dataset, BenchmarkDocument document, out string contentType, out string fileName)
        {
            if (!string.IsNullOrEmpty(document.File))
            {
                string path = Path.Combine(dataset.BaseDirectory, document.File);
                if (!System.IO.File.Exists(path)) throw new FileNotFoundException("Document file not found: " + path);
                fileName = Path.GetFileName(path);
                contentType = document.ContentType ?? ContentTypeFor(fileName);
                return System.IO.File.ReadAllBytes(path);
            }

            fileName = document.Id + ".txt";
            contentType = document.ContentType ?? "text/plain";
            if (contentType == "text/markdown") fileName = document.Id + ".md";
            string text = document.Body ?? string.Empty;
            if (!string.IsNullOrEmpty(document.Title) && !text.StartsWith(document.Title, StringComparison.Ordinal)) text = document.Title + "\n\n" + text;
            return new UTF8Encoding(false).GetBytes(text);
        }

        /// <summary>
        /// Content type from a file extension.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <returns>MIME type.</returns>
        public static string ContentTypeFor(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLowerInvariant())
            {
                case ".pdf": return "application/pdf";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".html":
                case ".htm": return "text/html";
                case ".md": return "text/markdown";
                case ".json": return "application/json";
                case ".csv": return "text/csv";
                default: return "text/plain";
            }
        }

        /// <summary>
        /// SHA-256 hex of bytes.
        /// </summary>
        /// <param name="bytes">Bytes.</param>
        /// <returns>Lowercase hex.</returns>
        public static string Sha256(byte[] bytes)
        {
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }

        /// <summary>
        /// SHA-256 hex of a string.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <returns>Lowercase hex.</returns>
        public static string Sha256(string text)
        {
            return Sha256(Encoding.UTF8.GetBytes(text));
        }

        #endregion
    }
}
