namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Test.Shared;

    internal class DockerDeploymentSuite : SuiteBase
    {
        public async Task<IReadOnlyList<AutomatedTestResult>> RunAsync()
        {
            ClearResults();

            await ExecuteTestAsync("Docker deployment: compose uses shared PostgreSQL initializer", async () =>
            {
                string root = GetRepositoryRoot();
                string compose = File.ReadAllText(Path.Combine(root, "docker", "compose.yaml"));

                AssertHelper.StringContains(compose, "postgres:", "postgres service");
                AssertHelper.StringContains(compose, "image: pgvector/pgvector:pg17", "pinned PostgreSQL image");
                AssertHelper.StringContains(compose, "postgres-init:", "postgres-init service");
                AssertHelper.StringContains(compose, "condition: service_completed_successfully", "postgres-init dependency condition");
                AssertHelper.StringContains(compose, "postgres-data:", "postgres-data volume");
                AssertHelper.StringContains(compose, "./postgres/init.sh:/init/postgres-init.sh:ro", "postgres init script mount");

                AssertHelper.IsFalse(compose.Contains("assistanthub-db-init", StringComparison.Ordinal), "compose should not define assistanthub-db-init");
                AssertHelper.IsFalse(compose.Contains("recalldb-init:", StringComparison.Ordinal), "compose should not define recalldb-init");
                AssertHelper.IsFalse(compose.Contains("./less3/less3.db:/app/less3.db", StringComparison.Ordinal), "Less3 SQLite mount removed");
                AssertHelper.IsFalse(compose.Contains("./partio/data/:/app/data/", StringComparison.Ordinal), "Partio SQLite data mount removed");
                AssertHelper.IsFalse(compose.Contains("./assistanthub/data/:/app/data/", StringComparison.Ordinal), "AssistantHub SQLite data mount removed");
            });

            await ExecuteTestAsync("Docker deployment: service configs use PostgreSQL databases", async () =>
            {
                string root = GetRepositoryRoot();

                AssertDatabaseConfig(root, Path.Combine("docker", "assistanthub", "assistanthub.json"), true, "assistanthub", "assistanthub_app", "assistanthub_password");
                AssertDatabaseConfig(root, Path.Combine("docker", "partio", "partio.json"), true, "partio", "partio_app", "partio_password");
                AssertDatabaseConfig(root, Path.Combine("docker", "less3", "system.json"), true, "less3", "less3_app", "less3_password");
                AssertDatabaseConfig(root, Path.Combine("docker", "recalldb", "recalldb.json"), false, "recalldb", "recalldb_app", "recalldb_password");
                AssertDatabaseConfig(root, Path.Combine("docker", "factory", "assistanthub.json"), true, "assistanthub", "assistanthub_app", "assistanthub_password");
                AssertDatabaseConfig(root, Path.Combine("docker", "factory", "partio.json"), true, "partio", "partio_app", "partio_password");
                AssertDatabaseConfig(root, Path.Combine("docker", "factory", "less3.system.json"), true, "less3", "less3_app", "less3_password");
                AssertDatabaseConfig(root, Path.Combine("docker", "factory", "recalldb.json"), false, "recalldb", "recalldb_app", "recalldb_password");
            });

            await ExecuteTestAsync("Docker deployment: factory reset is PostgreSQL first", async () =>
            {
                string root = GetRepositoryRoot();
                string resetBat = File.ReadAllText(Path.Combine(root, "docker", "factory", "reset.bat"));
                string resetSh = File.ReadAllText(Path.Combine(root, "docker", "factory", "reset.sh"));
                string factoryDirectory = Path.Combine(root, "docker", "factory");

                AssertHelper.StringContains(resetBat, "docker_postgres-data", "Windows reset postgres volume");
                AssertHelper.StringContains(resetSh, "docker_postgres-data", "POSIX reset postgres volume");
                AssertHelper.StringContains(resetBat, "less3.system.json", "Windows reset Less3 config");
                AssertHelper.StringContains(resetSh, "less3.system.json", "POSIX reset Less3 config");
                AssertHelper.StringContains(resetBat, "recalldb.json", "Windows reset RecallDB config");
                AssertHelper.StringContains(resetSh, "recalldb.json", "POSIX reset RecallDB config");

                AssertHelper.IsFalse(resetBat.Contains("copy /y \"%FACTORY_DIR%assistanthub.db\"", StringComparison.OrdinalIgnoreCase), "Windows reset should not copy AssistantHub SQLite factory DB");
                AssertHelper.IsFalse(resetBat.Contains("copy /y \"%FACTORY_DIR%less3.db\"", StringComparison.OrdinalIgnoreCase), "Windows reset should not copy Less3 SQLite factory DB");
                AssertHelper.IsFalse(resetBat.Contains("copy /y \"%FACTORY_DIR%partio.db\"", StringComparison.OrdinalIgnoreCase), "Windows reset should not copy Partio SQLite factory DB");
                AssertHelper.IsFalse(resetSh.Contains("cp \"$FACTORY_DIR/assistanthub.db\"", StringComparison.Ordinal), "POSIX reset should not copy AssistantHub SQLite factory DB");
                AssertHelper.IsFalse(resetSh.Contains("cp \"$FACTORY_DIR/less3.db\"", StringComparison.Ordinal), "POSIX reset should not copy Less3 SQLite factory DB");
                AssertHelper.IsFalse(resetSh.Contains("cp \"$FACTORY_DIR/partio.db\"", StringComparison.Ordinal), "POSIX reset should not copy Partio SQLite factory DB");

                AssertHelper.IsFalse(File.Exists(Path.Combine(factoryDirectory, "assistanthub.db")), "AssistantHub factory SQLite DB removed");
                AssertHelper.IsFalse(File.Exists(Path.Combine(factoryDirectory, "less3.db")), "Less3 factory SQLite DB removed");
                AssertHelper.IsFalse(File.Exists(Path.Combine(factoryDirectory, "partio.db")), "Partio factory SQLite DB removed");
            });

            await ExecuteTestAsync("Docker deployment: dashboard entrypoint is not browser-cached", async () =>
            {
                string root = GetRepositoryRoot();
                string dashboardNginx = File.ReadAllText(Path.Combine(root, "dashboard", "nginx.conf"));
                string composeNginx = File.ReadAllText(Path.Combine(root, "docker", "dashboard-nginx.conf"));

                AssertDashboardNoCacheHeaders(dashboardNginx, "dashboard nginx");
                AssertDashboardNoCacheHeaders(composeNginx, "compose dashboard nginx");
            });

            await ExecuteTestAsync("PostgreSQL assistant settings: boolean columns use PostgreSQL literals", async () =>
            {
                string root = GetRepositoryRoot();
                string source = File.ReadAllText(Path.Combine(
                    root,
                    "src",
                    "AssistantHub.Core",
                    "Database",
                    "Postgresql",
                    "Implementations",
                    "AssistantSettingsMethods.cs"));

                AssertHelper.StringContains(source, "FormatBooleanColumn(assistantSettings.EnableQueryRewrite)", "query rewrite boolean literal");
                AssertHelper.StringContains(source, "FormatBooleanColumn(assistantSettings.EnableReranking)", "reranking boolean literal");
                AssertHelper.StringContains(source, "FormatBooleanColumn(assistantSettings.EnableSlack)", "slack boolean literal");
                AssertHelper.StringContains(source, "return value ? \"TRUE\" : \"FALSE\";", "PostgreSQL boolean formatter");

                AssertHelper.IsFalse(source.Contains("EnableQueryRewrite ? 1 : 0", StringComparison.Ordinal), "query rewrite should not use integer boolean");
                AssertHelper.IsFalse(source.Contains("EnableReranking ? 1 : 0", StringComparison.Ordinal), "reranking should not use integer boolean");
                AssertHelper.IsFalse(source.Contains("EnableSlack ? 1 : 0", StringComparison.Ordinal), "slack should not use integer boolean");
            });

            return GetResults();
        }

        private static void AssertDatabaseConfig(
            string root,
            string relativePath,
            bool requiresType,
            string expectedDatabase,
            string expectedUsername,
            string expectedPassword)
        {
            string path = Path.Combine(root, relativePath);
            string json = File.ReadAllText(path);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement database = document.RootElement.GetProperty("Database");

            if (requiresType)
            {
                AssertHelper.AreEqual("Postgresql", database.GetProperty("Type").GetString(), relativePath + " database type");
                AssertHelper.AreEqual(String.Empty, database.GetProperty("Filename").GetString(), relativePath + " database filename");
            }

            AssertHelper.AreEqual("postgres", database.GetProperty("Hostname").GetString(), relativePath + " database host");
            AssertHelper.AreEqual(5432, database.GetProperty("Port").GetInt32(), relativePath + " database port");
            AssertHelper.AreEqual(expectedDatabase, database.GetProperty("DatabaseName").GetString(), relativePath + " database name");
            AssertHelper.AreEqual(expectedUsername, database.GetProperty("Username").GetString(), relativePath + " database username");
            AssertHelper.AreEqual(expectedPassword, database.GetProperty("Password").GetString(), relativePath + " database password");
            AssertHelper.IsFalse(String.Equals("postgres", database.GetProperty("Username").GetString(), StringComparison.OrdinalIgnoreCase), relativePath + " should not use the superuser");
        }

        private static void AssertDashboardNoCacheHeaders(string nginx, string name)
        {
            AssertHelper.StringContains(nginx, "location = /index.html", name + " index cache policy route");
            AssertHelper.StringContains(nginx, "location = /config.js", name + " runtime config cache policy route");
            AssertHelper.StringContains(nginx, "no-store, no-cache", name + " no-cache header");
        }

        private static string GetRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                bool hasDocker = Directory.Exists(Path.Combine(directory.FullName, "docker"));
                bool hasSource = Directory.Exists(Path.Combine(directory.FullName, "src"));
                bool hasReadme = File.Exists(Path.Combine(directory.FullName, "README.md"));

                if (hasDocker && hasSource && hasReadme)
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate the AssistantHub repository root.");
        }
    }
}
