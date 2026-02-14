namespace AssistantHub.Core.Settings
{
    using System;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Database settings.
    /// </summary>
    public class DatabaseSettings
    {
        #region Public-Members

        /// <summary>
        /// Database type.
        /// </summary>
        public DatabaseTypeEnum Type { get; set; } = DatabaseTypeEnum.Sqlite;

        /// <summary>
        /// Database filename, for use with Sqlite.
        /// </summary>
        public string Filename
        {
            get => _Filename;
            set { if (!String.IsNullOrEmpty(value)) _Filename = value; }
        }

        /// <summary>
        /// Database server hostname.
        /// </summary>
        public string Hostname
        {
            get => _Hostname;
            set { if (!String.IsNullOrEmpty(value)) _Hostname = value; }
        }

        /// <summary>
        /// Database server port.
        /// </summary>
        public int Port
        {
            get => _Port;
            set => _Port = (value >= 0 && value <= 65535) ? value : throw new ArgumentOutOfRangeException(nameof(Port));
        }

        /// <summary>
        /// Database name.
        /// </summary>
        public string DatabaseName
        {
            get => _DatabaseName;
            set { if (!String.IsNullOrEmpty(value)) _DatabaseName = value; }
        }

        /// <summary>
        /// Database username.
        /// </summary>
        public string Username
        {
            get => _Username;
            set { if (!String.IsNullOrEmpty(value)) _Username = value; }
        }

        /// <summary>
        /// Database password.
        /// </summary>
        public string Password
        {
            get => _Password;
            set { if (!String.IsNullOrEmpty(value)) _Password = value; }
        }

        /// <summary>
        /// Database schema.
        /// </summary>
        public string Schema
        {
            get => _Schema;
            set { if (!String.IsNullOrEmpty(value)) _Schema = value; }
        }

        /// <summary>
        /// Require encryption for the database connection.
        /// </summary>
        public bool RequireEncryption { get; set; } = false;

        /// <summary>
        /// Enable or disable query logging.
        /// </summary>
        public bool LogQueries { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Filename = "assistanthub.db";
        private string _Hostname = "localhost";
        private int _Port = 5432;
        private string _DatabaseName = "assistanthub";
        private string _Username = "postgres";
        private string _Password = "password";
        private string _Schema = "public";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DatabaseSettings()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the connection string based on the configured database type.
        /// </summary>
        /// <returns>Connection string.</returns>
        public string GetConnectionString()
        {
            switch (Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return "Data Source=" + _Filename;

                case DatabaseTypeEnum.Postgresql:
                    return
                        "Host=" + _Hostname + ";" +
                        "Port=" + _Port + ";" +
                        "Database=" + _DatabaseName + ";" +
                        "Username=" + _Username + ";" +
                        "Password=" + _Password + ";" +
                        "Search Path=" + _Schema + ";" +
                        "SSL Mode=" + (RequireEncryption ? "Require" : "Prefer");

                case DatabaseTypeEnum.SqlServer:
                    return
                        "Server=" + _Hostname + "," + _Port + ";" +
                        "Database=" + _DatabaseName + ";" +
                        "User Id=" + _Username + ";" +
                        "Password=" + _Password + ";" +
                        "Encrypt=" + RequireEncryption.ToString();

                case DatabaseTypeEnum.Mysql:
                    return
                        "Server=" + _Hostname + ";" +
                        "Port=" + _Port + ";" +
                        "Database=" + _DatabaseName + ";" +
                        "Uid=" + _Username + ";" +
                        "Pwd=" + _Password + ";" +
                        "SslMode=" + (RequireEncryption ? "Required" : "Preferred");

                default:
                    throw new ArgumentException("Unknown database type: " + Type.ToString());
            }
        }

        #endregion
    }
}
