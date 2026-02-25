namespace AssistantHub.Core
{
    using System;

    /// <summary>
    /// Global constants.
    /// </summary>
    public static class Constants
    {
        #region General

        /// <summary>
        /// Product name.
        /// </summary>
        public static string ProductName = "AssistantHub";

        /// <summary>
        /// Product version.
        /// </summary>
        public static string ProductVersion = "1.0.0";

        /// <summary>
        /// Logo.
        /// </summary>
        public static string Logo =
            Environment.NewLine +
            @"                 _      _              _   _           _     " + Environment.NewLine +
            @"   __ _ ___ ___ (_)____| |_ __ _ _ __ | |_| |__  _   _| |__  " + Environment.NewLine +
            @"  / _` / __/ __|| / ___|  _/ _` | '_ \| __| '_ \| | | | '_ \ " + Environment.NewLine +
            @" | (_| \__ \__ \| \__ \| || (_| | | | | |_| | | | |_| | |_) |" + Environment.NewLine +
            @"  \__,_|___/___/|_|___/ \__\__,_|_| |_|\__|_| |_|\__,_|_.__/ " + Environment.NewLine;

        #endregion

        #region File-Paths

        /// <summary>
        /// Settings file.
        /// </summary>
        public static string SettingsFile = "assistanthub.json";

        /// <summary>
        /// Log directory.
        /// </summary>
        public static string LogDirectory = "./logs/";

        #endregion

        #region Identifier-Prefixes

        /// <summary>
        /// User identifier prefix.
        /// </summary>
        public static string UserIdentifierPrefix = "usr_";

        /// <summary>
        /// Credential identifier prefix.
        /// </summary>
        public static string CredentialIdentifierPrefix = "cred_";

        /// <summary>
        /// Assistant identifier prefix.
        /// </summary>
        public static string AssistantIdentifierPrefix = "asst_";

        /// <summary>
        /// Assistant settings identifier prefix.
        /// </summary>
        public static string AssistantSettingsIdentifierPrefix = "aset_";

        /// <summary>
        /// Assistant document identifier prefix.
        /// </summary>
        public static string AssistantDocumentIdentifierPrefix = "adoc_";

        /// <summary>
        /// Assistant feedback identifier prefix.
        /// </summary>
        public static string AssistantFeedbackIdentifierPrefix = "afb_";

        /// <summary>
        /// Ingestion rule identifier prefix.
        /// </summary>
        public static string IngestionRuleIdentifierPrefix = "irule_";

        /// <summary>
        /// Thread identifier prefix.
        /// </summary>
        public static string ThreadIdentifierPrefix = "thr_";

        /// <summary>
        /// Chat history identifier prefix.
        /// </summary>
        public static string ChatHistoryIdentifierPrefix = "chist_";

        /// <summary>
        /// Tenant identifier prefix.
        /// </summary>
        public static string TenantIdentifierPrefix = "ten_";

        /// <summary>
        /// Chat completion identifier prefix.
        /// </summary>
        public static string ChatCompletionIdentifierPrefix = "chatcmpl-";

        /// <summary>
        /// Identifier length (total length including prefix).
        /// </summary>
        public static int IdentifierLength = 32;

        /// <summary>
        /// Bearer token length.
        /// </summary>
        public static int BearerTokenLength = 64;

        #endregion

        #region Database-Tables

        /// <summary>
        /// Users table name.
        /// </summary>
        public static string UsersTable = "users";

        /// <summary>
        /// Credentials table name.
        /// </summary>
        public static string CredentialsTable = "credentials";

        /// <summary>
        /// Assistants table name.
        /// </summary>
        public static string AssistantsTable = "assistants";

        /// <summary>
        /// Assistant settings table name.
        /// </summary>
        public static string AssistantSettingsTable = "assistant_settings";

        /// <summary>
        /// Assistant documents table name.
        /// </summary>
        public static string AssistantDocumentsTable = "assistant_documents";

        /// <summary>
        /// Assistant feedback table name.
        /// </summary>
        public static string AssistantFeedbackTable = "assistant_feedback";

        /// <summary>
        /// Ingestion rules table name.
        /// </summary>
        public static string IngestionRulesTable = "ingestion_rules";

        /// <summary>
        /// Chat history table name.
        /// </summary>
        public static string ChatHistoryTable = "chat_history";

        /// <summary>
        /// Tenants table name.
        /// </summary>
        public static string TenantsTable = "tenants";

        #endregion

        #region Headers

        /// <summary>
        /// Thread ID header name.
        /// </summary>
        public static string ThreadIdHeader = "X-Thread-ID";

        #endregion

        #region Default-Admin

        /// <summary>
        /// Default tenant identifier.
        /// </summary>
        public static string DefaultTenantId = "default";

        /// <summary>
        /// Default tenant name.
        /// </summary>
        public static string DefaultTenantName = "Default Tenant";

        /// <summary>
        /// Default admin email.
        /// </summary>
        public static string DefaultAdminEmail = "admin@assistanthub";

        /// <summary>
        /// Default admin password.
        /// </summary>
        public static string DefaultAdminPassword = "password";

        /// <summary>
        /// Default admin first name.
        /// </summary>
        public static string DefaultAdminFirstName = "Admin";

        /// <summary>
        /// Default admin last name.
        /// </summary>
        public static string DefaultAdminLastName = "User";

        #endregion
    }
}
