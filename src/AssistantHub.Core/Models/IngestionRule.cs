namespace AssistantHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.Json;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// Ingestion rule record.
    /// </summary>
    public class IngestionRule
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix irule_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId
        {
            get => _TenantId;
            set => _TenantId = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(TenantId));
        }

        /// <summary>
        /// Display name for the ingestion rule.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Name));
        }

        /// <summary>
        /// Description of the ingestion rule.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// S3 bucket name for document storage.
        /// </summary>
        public string Bucket
        {
            get => _Bucket;
            set => _Bucket = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Bucket));
        }

        /// <summary>
        /// RecallDB collection display name.
        /// </summary>
        public string CollectionName
        {
            get => _CollectionName;
            set => _CollectionName = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(CollectionName));
        }

        /// <summary>
        /// RecallDB collection GUID for API calls.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Labels to apply to ingested documents.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Tags to apply to ingested documents.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = null;

        /// <summary>
        /// Atomization configuration (placeholder).
        /// </summary>
        public Dictionary<string, object> Atomization { get; set; } = null;

        /// <summary>
        /// Summarization configuration.
        /// </summary>
        public IngestionSummarizationConfig Summarization { get; set; } = null;

        /// <summary>
        /// Chunking configuration.
        /// </summary>
        public IngestionChunkingConfig Chunking { get; set; } = null;

        /// <summary>
        /// Embedding configuration.
        /// </summary>
        public IngestionEmbeddingConfig Embedding { get; set; } = null;

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewIngestionRuleId();
        private string _TenantId = Constants.DefaultTenantId;
        private string _Name = "Untitled Rule";
        private string _Bucket = "default";
        private string _CollectionName = "default";

        private static JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public IngestionRule()
        {
        }

        /// <summary>
        /// Create an IngestionRule from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>IngestionRule instance or null.</returns>
        public static IngestionRule FromDataRow(DataRow row)
        {
            if (row == null) return null;
            IngestionRule obj = new IngestionRule();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.TenantId = DataTableHelper.GetStringValue(row, "tenant_id");
            obj.Name = DataTableHelper.GetStringValue(row, "name");
            obj.Description = DataTableHelper.GetStringValue(row, "description");
            obj.Bucket = DataTableHelper.GetStringValue(row, "bucket");
            obj.CollectionName = DataTableHelper.GetStringValue(row, "collection_name");
            obj.CollectionId = DataTableHelper.GetStringValue(row, "collection_id");

            string labelsJson = DataTableHelper.GetStringValue(row, "labels_json");
            if (!String.IsNullOrEmpty(labelsJson))
            {
                try { obj.Labels = JsonSerializer.Deserialize<List<string>>(labelsJson, _JsonOptions); }
                catch { }
            }

            string tagsJson = DataTableHelper.GetStringValue(row, "tags_json");
            if (!String.IsNullOrEmpty(tagsJson))
            {
                try { obj.Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson, _JsonOptions); }
                catch { }
            }

            string atomizationJson = DataTableHelper.GetStringValue(row, "atomization_json");
            if (!String.IsNullOrEmpty(atomizationJson))
            {
                try { obj.Atomization = JsonSerializer.Deserialize<Dictionary<string, object>>(atomizationJson, _JsonOptions); }
                catch { }
            }

            string summarizationJson = DataTableHelper.GetStringValue(row, "summarization_json");
            if (!String.IsNullOrEmpty(summarizationJson))
            {
                try { obj.Summarization = JsonSerializer.Deserialize<IngestionSummarizationConfig>(summarizationJson, _JsonOptions); }
                catch { }
            }

            string chunkingJson = DataTableHelper.GetStringValue(row, "chunking_json");
            if (!String.IsNullOrEmpty(chunkingJson))
            {
                try { obj.Chunking = JsonSerializer.Deserialize<IngestionChunkingConfig>(chunkingJson, _JsonOptions); }
                catch { }
            }

            string embeddingJson = DataTableHelper.GetStringValue(row, "embedding_json");
            if (!String.IsNullOrEmpty(embeddingJson))
            {
                try { obj.Embedding = JsonSerializer.Deserialize<IngestionEmbeddingConfig>(embeddingJson, _JsonOptions); }
                catch { }
            }

            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion
    }
}
