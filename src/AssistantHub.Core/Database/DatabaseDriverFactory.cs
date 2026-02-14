namespace AssistantHub.Core.Database
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Settings;
    using AssistantHub.Core.Database.Sqlite;
    using AssistantHub.Core.Database.Postgresql;
    using AssistantHub.Core.Database.SqlServer;
    using AssistantHub.Core.Database.Mysql;
    using SyslogLogging;

    /// <summary>
    /// Database driver factory.
    /// </summary>
    public static class DatabaseDriverFactory
    {
        /// <summary>
        /// Create a database driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <returns>Database driver.</returns>
        public static DatabaseDriverBase Create(DatabaseSettings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            switch (settings.Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return new SqliteDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.Postgresql:
                    return new PostgresqlDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.SqlServer:
                    return new SqlServerDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.Mysql:
                    return new MysqlDatabaseDriver(settings, logging);
                default:
                    throw new ArgumentException("Unknown database type: " + settings.Type.ToString());
            }
        }

        /// <summary>
        /// Create and initialize a database driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Database driver.</returns>
        public static async Task<DatabaseDriverBase> CreateAndInitializeAsync(DatabaseSettings settings, LoggingModule logging, CancellationToken token = default)
        {
            DatabaseDriverBase driver = Create(settings, logging);
            await driver.InitializeAsync(token).ConfigureAwait(false);
            return driver;
        }
    }
}
