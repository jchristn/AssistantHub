namespace Test.Services.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Settings;
    using SyslogLogging;
    using Test.Common;

    public static class ProcessingLogServiceTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("ProcessingLogServiceTests");

            string baseDir = Path.Combine(Path.GetTempPath(), "assistanthub_test_proclogs_" + Guid.NewGuid().ToString("N"));

            try
            {
                await runner.RunTestAsync("ProcessingLog: constructor creates directory", async ct =>
                {
                    string dir = Path.Combine(baseDir, "ctor_test");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;

                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    AssertHelper.IsTrue(Directory.Exists(dir), "directory should exist after construction");
                }, token);

                await runner.RunTestAsync("ProcessingLog: constructor throws on null settings", async ct =>
                {
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                    {
                        new ProcessingLogService(null, logging);
                    }, "should throw on null settings");
                }, token);

                await runner.RunTestAsync("ProcessingLog: constructor throws on null logging", async ct =>
                {
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = Path.Combine(baseDir, "null_log_test");
                    AssertHelper.ThrowsAsync<ArgumentNullException>(async () =>
                    {
                        new ProcessingLogService(settings, null);
                    }, "should throw on null logging");
                }, token);

                await runner.RunTestAsync("ProcessingLog: LogAsync writes log file", async ct =>
                {
                    string dir = Path.Combine(baseDir, "log_write");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    await svc.LogAsync("doc123", "INFO", "test message");

                    string filePath = Path.Combine(dir, "doc123.log");
                    AssertHelper.IsTrue(File.Exists(filePath), "log file should exist");
                    string contents = await File.ReadAllTextAsync(filePath);
                    AssertHelper.StringContains(contents, "INFO", "should contain level");
                    AssertHelper.StringContains(contents, "test message", "should contain message");
                }, token);

                await runner.RunTestAsync("ProcessingLog: LogAsync with null documentId does nothing", async ct =>
                {
                    string dir = Path.Combine(baseDir, "null_docid");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    await svc.LogAsync(null, "INFO", "should not write");
                    await svc.LogAsync("", "INFO", "should not write either");

                    string[] files = Directory.GetFiles(dir, "*.log", SearchOption.AllDirectories);
                    AssertHelper.AreEqual(0, files.Length, "no log files should be created");
                }, token);

                await runner.RunTestAsync("ProcessingLog: LogAsync with tenantId creates tenant subdirectory", async ct =>
                {
                    string dir = Path.Combine(baseDir, "tenant_ns");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    await svc.LogAsync("doc456", "INFO", "tenant log", "ten_abc");

                    string filePath = Path.Combine(dir, "ten_abc", "doc456.log");
                    AssertHelper.IsTrue(File.Exists(filePath), "log file should be in tenant subdirectory");
                }, token);

                await runner.RunTestAsync("ProcessingLog: GetLogAsync reads written log", async ct =>
                {
                    string dir = Path.Combine(baseDir, "get_log");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    await svc.LogAsync("doc789", "WARN", "warning message");
                    string content = await svc.GetLogAsync("doc789");

                    AssertHelper.IsNotNull(content, "should return log content");
                    AssertHelper.StringContains(content, "WARN", "should contain level");
                    AssertHelper.StringContains(content, "warning message", "should contain message");
                }, token);

                await runner.RunTestAsync("ProcessingLog: GetLogAsync returns null for non-existent doc", async ct =>
                {
                    string dir = Path.Combine(baseDir, "no_log");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    string content = await svc.GetLogAsync("nonexistent");
                    AssertHelper.IsNull(content, "should return null");
                }, token);

                await runner.RunTestAsync("ProcessingLog: GetLogAsync with null documentId returns null", async ct =>
                {
                    string dir = Path.Combine(baseDir, "null_get");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    string content = await svc.GetLogAsync(null);
                    AssertHelper.IsNull(content, "should return null for null docId");
                }, token);

                await runner.RunTestAsync("ProcessingLog: GetLogAsync with tenant fallback to flat path", async ct =>
                {
                    string dir = Path.Combine(baseDir, "fallback");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    // Write without tenant (flat path)
                    await svc.LogAsync("docfallback", "INFO", "flat log");

                    // Read with tenant - should fallback to flat path
                    string content = await svc.GetLogAsync("docfallback", "ten_xyz");
                    AssertHelper.IsNotNull(content, "should fallback to flat path");
                    AssertHelper.StringContains(content, "flat log", "should contain the flat log content");
                }, token);

                await runner.RunTestAsync("ProcessingLog: LogStepStartAsync returns stopwatch", async ct =>
                {
                    string dir = Path.Combine(baseDir, "step_start");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    Stopwatch sw = await svc.LogStepStartAsync("docstep", "ChunkingStep");
                    AssertHelper.IsNotNull(sw, "should return a stopwatch");
                    AssertHelper.IsTrue(sw.IsRunning, "stopwatch should be running");

                    string content = await svc.GetLogAsync("docstep");
                    AssertHelper.StringContains(content, "ChunkingStep started", "should log step start");
                }, token);

                await runner.RunTestAsync("ProcessingLog: LogStepCompleteAsync logs elapsed time", async ct =>
                {
                    string dir = Path.Combine(baseDir, "step_complete");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    Stopwatch sw = await svc.LogStepStartAsync("doccomplete", "EmbeddingStep");
                    await Task.Delay(10);
                    await svc.LogStepCompleteAsync("doccomplete", "EmbeddingStep", "200 chunks", sw);

                    string content = await svc.GetLogAsync("doccomplete");
                    AssertHelper.StringContains(content, "EmbeddingStep completed", "should log completion");
                    AssertHelper.StringContains(content, "ms", "should contain elapsed time");
                    AssertHelper.StringContains(content, "200 chunks", "should contain result");
                }, token);

                await runner.RunTestAsync("ProcessingLog: CleanupOldLogsAsync removes old files", async ct =>
                {
                    string dir = Path.Combine(baseDir, "cleanup");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    settings.RetentionDays = 0; // 0 days = delete everything
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    // Write a log file and backdate it
                    await svc.LogAsync("oldlog", "INFO", "old entry");
                    string filePath = Path.Combine(dir, "oldlog.log");
                    File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow.AddDays(-2));

                    await svc.CleanupOldLogsAsync();

                    AssertHelper.IsFalse(File.Exists(filePath), "old log file should be deleted");
                }, token);

                await runner.RunTestAsync("ProcessingLog: CleanupOldLogsAsync keeps recent files", async ct =>
                {
                    string dir = Path.Combine(baseDir, "cleanup_keep");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    settings.RetentionDays = 30;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    await svc.LogAsync("recentlog", "INFO", "recent entry");
                    await svc.CleanupOldLogsAsync();

                    string filePath = Path.Combine(dir, "recentlog.log");
                    AssertHelper.IsTrue(File.Exists(filePath), "recent log file should be kept");
                }, token);

                await runner.RunTestAsync("ProcessingLog: multiple LogAsync calls append to same file", async ct =>
                {
                    string dir = Path.Combine(baseDir, "append");
                    ProcessingLogSettings settings = new ProcessingLogSettings();
                    settings.Directory = dir;
                    LoggingModule logging = new LoggingModule();
                    logging.Settings.EnableConsole = false;
                    ProcessingLogService svc = new ProcessingLogService(settings, logging);

                    await svc.LogAsync("appenddoc", "INFO", "first line");
                    await svc.LogAsync("appenddoc", "WARN", "second line");
                    await svc.LogAsync("appenddoc", "ERROR", "third line");

                    string content = await svc.GetLogAsync("appenddoc");
                    AssertHelper.StringContains(content, "first line", "should contain first line");
                    AssertHelper.StringContains(content, "second line", "should contain second line");
                    AssertHelper.StringContains(content, "third line", "should contain third line");
                }, token);
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    if (Directory.Exists(baseDir))
                        Directory.Delete(baseDir, true);
                }
                catch { }
            }
        }
    }
}
