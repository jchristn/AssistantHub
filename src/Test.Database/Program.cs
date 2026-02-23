namespace Test.Database
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using Test.Database.Tests;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            DatabaseSettings settings = ParseArguments(args);
            if (settings == null) return 1;

            Console.WriteLine("==========================================================");
            Console.WriteLine("  AssistantHub Database Driver Test Suite");
            Console.WriteLine("==========================================================");
            Console.WriteLine($"  Database Type:  {settings.Type}");

            if (settings.Type == DatabaseTypeEnum.Sqlite)
            {
                Console.WriteLine($"  Filename:       {settings.Filename}");
            }
            else
            {
                Console.WriteLine($"  Host:           {settings.Hostname}");
                Console.WriteLine($"  Port:           {settings.Port}");
                Console.WriteLine($"  Database:       {settings.DatabaseName}");
                Console.WriteLine($"  Username:       {settings.Username}");
                Console.WriteLine($"  Schema:         {settings.Schema}");
            }

            Console.WriteLine("==========================================================");
            Console.WriteLine();

            LoggingModule logging = new LoggingModule();
            logging.Settings.EnableConsole = false;

            DatabaseDriverBase driver = null;
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            try
            {
                Console.WriteLine("Initializing database driver...");
                driver = await DatabaseDriverFactory.CreateAndInitializeAsync(settings, logging);
                Console.WriteLine("Database driver initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to initialize database driver: {ex.Message}");
                Console.ResetColor();
                return 1;
            }

            TestRunner runner = new TestRunner();
            CancellationToken token = CancellationToken.None;

            try
            {
                await UserTests.RunAllAsync(driver, runner, token);
                await CredentialTests.RunAllAsync(driver, runner, token);
                await AssistantTests.RunAllAsync(driver, runner, token);
                await AssistantSettingsTests.RunAllAsync(driver, runner, token);
                await AssistantDocumentTests.RunAllAsync(driver, runner, token);
                await AssistantFeedbackTests.RunAllAsync(driver, runner, token);
                await IngestionRuleTests.RunAllAsync(driver, runner, token);
                await ChatHistoryTests.RunAllAsync(driver, runner, token);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unhandled exception during test execution: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            totalStopwatch.Stop();
            runner.PrintSummary(totalStopwatch.Elapsed.TotalMilliseconds);

            // cleanup SQLite test file
            if (settings.Type == DatabaseTypeEnum.Sqlite && File.Exists(settings.Filename))
            {
                try { File.Delete(settings.Filename); } catch { }
            }

            // return exit code based on test results
            foreach (TestResult r in runner.Results)
            {
                if (!r.Passed) return 1;
            }

            return 0;
        }

        private static DatabaseSettings ParseArguments(string[] args)
        {
            DatabaseSettings settings = new DatabaseSettings();
            bool typeSpecified = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();

                switch (arg)
                {
                    case "--type":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --type"); return null; }
                        i++;
                        switch (args[i].ToLowerInvariant())
                        {
                            case "sqlite": settings.Type = DatabaseTypeEnum.Sqlite; break;
                            case "mysql": settings.Type = DatabaseTypeEnum.Mysql; break;
                            case "sqlserver": settings.Type = DatabaseTypeEnum.SqlServer; break;
                            case "postgres":
                            case "postgresql": settings.Type = DatabaseTypeEnum.Postgresql; break;
                            default: PrintUsage($"Unknown database type: {args[i]}"); return null;
                        }
                        typeSpecified = true;
                        break;

                    case "--host":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --host"); return null; }
                        settings.Hostname = args[++i];
                        break;

                    case "--port":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --port"); return null; }
                        if (!int.TryParse(args[++i], out int port)) { PrintUsage("Invalid port number"); return null; }
                        settings.Port = port;
                        break;

                    case "--user":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --user"); return null; }
                        settings.Username = args[++i];
                        break;

                    case "--pass":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --pass"); return null; }
                        settings.Password = args[++i];
                        break;

                    case "--name":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --name"); return null; }
                        settings.DatabaseName = args[++i];
                        break;

                    case "--schema":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --schema"); return null; }
                        settings.Schema = args[++i];
                        break;

                    case "--filename":
                        if (i + 1 >= args.Length) { PrintUsage("Missing value for --filename"); return null; }
                        settings.Filename = args[++i];
                        break;

                    case "--help":
                    case "-h":
                        PrintUsage();
                        return null;

                    default:
                        PrintUsage($"Unknown argument: {args[i]}");
                        return null;
                }
            }

            if (!typeSpecified)
            {
                PrintUsage("--type is required");
                return null;
            }

            // set sensible port defaults if not explicitly specified
            if (settings.Type == DatabaseTypeEnum.Mysql && !HasArgument(args, "--port"))
                settings.Port = 3306;
            else if (settings.Type == DatabaseTypeEnum.SqlServer && !HasArgument(args, "--port"))
                settings.Port = 1433;
            else if (settings.Type == DatabaseTypeEnum.Postgresql && !HasArgument(args, "--port"))
                settings.Port = 5432;

            // for SQLite, use a test-specific filename to avoid conflicts
            if (settings.Type == DatabaseTypeEnum.Sqlite && !HasArgument(args, "--filename"))
                settings.Filename = "test_database_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".db";

            return settings;
        }

        private static bool HasArgument(string[] args, string name)
        {
            foreach (string a in args)
            {
                if (a.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void PrintUsage(string error = null)
        {
            if (error != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {error}");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.WriteLine("AssistantHub Database Driver Test Suite");
            Console.WriteLine();
            Console.WriteLine("  Exhaustively tests every database driver method across all object types");
            Console.WriteLine("  (User, Credential, Assistant, AssistantSettings, AssistantDocument,");
            Console.WriteLine("  AssistantFeedback, IngestionRule, ChatHistory) including create, read,");
            Console.WriteLine("  update, delete, enumeration, pagination, ordering, and filtering.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  Test.Database --type <dbtype> [options]");
            Console.WriteLine();
            Console.WriteLine("Required:");
            Console.WriteLine("  --type <type>       Database type: sqlite, mysql, sqlserver, postgres");
            Console.WriteLine();
            Console.WriteLine("Connection options (for server-based databases):");
            Console.WriteLine("  --host <hostname>   Database server hostname (default: localhost)");
            Console.WriteLine("  --port <port>       Database server port (default varies by type)");
            Console.WriteLine("  --user <username>   Database username");
            Console.WriteLine("  --pass <password>   Database password");
            Console.WriteLine("  --name <dbname>     Database name (default: assistanthub)");
            Console.WriteLine("  --schema <schema>   Database schema (default: public, PostgreSQL only)");
            Console.WriteLine();
            Console.WriteLine("SQLite options:");
            Console.WriteLine("  --filename <file>   SQLite database file path (default: auto-generated temp file)");
            Console.WriteLine();
            Console.WriteLine("Default ports by database type:");
            Console.WriteLine("  PostgreSQL:  5432");
            Console.WriteLine("  MySQL:       3306");
            Console.WriteLine("  SQL Server:  1433");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine();
            Console.WriteLine("  SQLite (no server required, uses a local file):");
            Console.WriteLine("    Test.Database --type sqlite");
            Console.WriteLine("    Test.Database --type sqlite --filename ./mytest.db");
            Console.WriteLine();
            Console.WriteLine("  MySQL:");
            Console.WriteLine("    Test.Database --type mysql --host 127.0.0.1 --port 3306 \\");
            Console.WriteLine("      --user root --pass mypassword --name assistanthubdb");
            Console.WriteLine();
            Console.WriteLine("  PostgreSQL:");
            Console.WriteLine("    Test.Database --type postgres --host 127.0.0.1 --port 5432 \\");
            Console.WriteLine("      --user postgres --pass mypassword --name assistanthubdb --schema public");
            Console.WriteLine();
            Console.WriteLine("  SQL Server:");
            Console.WriteLine("    Test.Database --type sqlserver --host 127.0.0.1 --port 1433 \\");
            Console.WriteLine("      --user sa --pass mypassword --name assistanthubdb");
        }
    }
}
