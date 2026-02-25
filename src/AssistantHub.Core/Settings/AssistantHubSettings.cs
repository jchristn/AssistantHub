namespace AssistantHub.Core.Settings
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// AssistantHub settings.
    /// </summary>
    public class AssistantHubSettings
    {
        #region Public-Members

        /// <summary>
        /// List of global admin API keys.
        /// </summary>
        public List<string> AdminApiKeys { get; set; } = new List<string> { "assistanthubadmin" };

        /// <summary>
        /// Default tenant settings for first-run provisioning.
        /// </summary>
        public DefaultTenantSettings DefaultTenant
        {
            get => _DefaultTenant;
            set { if (value != null) _DefaultTenant = value; }
        }

        /// <summary>
        /// Webserver settings.
        /// </summary>
        public WebserverSettings Webserver
        {
            get => _Webserver;
            set { if (value != null) _Webserver = value; }
        }

        /// <summary>
        /// Database settings.
        /// </summary>
        public DatabaseSettings Database
        {
            get => _Database;
            set { if (value != null) _Database = value; }
        }

        /// <summary>
        /// S3-compatible storage settings.
        /// </summary>
        public S3Settings S3
        {
            get => _S3;
            set { if (value != null) _S3 = value; }
        }

        /// <summary>
        /// DocumentAtom service settings.
        /// </summary>
        public DocumentAtomSettings DocumentAtom
        {
            get => _DocumentAtom;
            set { if (value != null) _DocumentAtom = value; }
        }

        /// <summary>
        /// Chunking service settings.
        /// </summary>
        public ChunkingSettings Chunking
        {
            get => _Chunking;
            set { if (value != null) _Chunking = value; }
        }

        /// <summary>
        /// Inference service settings.
        /// </summary>
        public InferenceSettings Inference
        {
            get => _Inference;
            set { if (value != null) _Inference = value; }
        }

        /// <summary>
        /// RecallDb service settings.
        /// </summary>
        public RecallDbSettings RecallDb
        {
            get => _RecallDb;
            set { if (value != null) _RecallDb = value; }
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get => _Logging;
            set { if (value != null) _Logging = value; }
        }

        /// <summary>
        /// Per-document processing log settings.
        /// </summary>
        public ProcessingLogSettings ProcessingLog
        {
            get => _ProcessingLog;
            set { if (value != null) _ProcessingLog = value; }
        }

        /// <summary>
        /// Chat history settings.
        /// </summary>
        public ChatHistorySettings ChatHistory
        {
            get => _ChatHistory;
            set { if (value != null) _ChatHistory = value; }
        }

        #endregion

        #region Private-Members

        private WebserverSettings _Webserver = new WebserverSettings();
        private DatabaseSettings _Database = new DatabaseSettings();
        private S3Settings _S3 = new S3Settings();
        private DocumentAtomSettings _DocumentAtom = new DocumentAtomSettings();
        private ChunkingSettings _Chunking = new ChunkingSettings();
        private InferenceSettings _Inference = new InferenceSettings();
        private RecallDbSettings _RecallDb = new RecallDbSettings();
        private LoggingSettings _Logging = new LoggingSettings();
        private ProcessingLogSettings _ProcessingLog = new ProcessingLogSettings();
        private ChatHistorySettings _ChatHistory = new ChatHistorySettings();
        private DefaultTenantSettings _DefaultTenant = new DefaultTenantSettings();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AssistantHubSettings()
        {
        }

        #endregion
    }
}
