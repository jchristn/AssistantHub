namespace AssistantHub.Core.Enums
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Database type enumeration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DatabaseTypeEnum
    {
        /// <summary>
        /// SQLite.
        /// </summary>
        [EnumMember(Value = "Sqlite")]
        Sqlite,

        /// <summary>
        /// PostgreSQL.
        /// </summary>
        [EnumMember(Value = "Postgresql")]
        Postgresql,

        /// <summary>
        /// SQL Server.
        /// </summary>
        [EnumMember(Value = "SqlServer")]
        SqlServer,

        /// <summary>
        /// MySQL.
        /// </summary>
        [EnumMember(Value = "Mysql")]
        Mysql
    }
}
