namespace Test.Services.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services.Crawlers;
    using SyslogLogging;
    using Test.Common;

    public static class CrawlerFactoryTests
    {
        public static async Task RunAllAsync(TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("CrawlerFactoryTests");

            MockDatabaseDriver db = new MockDatabaseDriver();
            LoggingModule logging = new LoggingModule();
            logging.Settings.EnableConsole = false;

            await runner.RunTestAsync("CrawlerFactory: Web type returns WebRepositoryCrawler", async ct =>
            {
                CrawlPlan plan = new CrawlPlan
                {
                    Name = "Test Plan",
                    RepositoryType = RepositoryTypeEnum.Web
                };
                CrawlOperation op = new CrawlOperation();

                CrawlerBase crawler = CrawlerFactory.Create(
                    RepositoryTypeEnum.Web,
                    logging,
                    db,
                    plan,
                    op,
                    null, // ingestion
                    null, // storage
                    null, // processingLog
                    System.IO.Path.GetTempPath(),
                    CancellationToken.None);

                AssertHelper.IsNotNull(crawler, "crawler should not be null");
                AssertHelper.IsTrue(crawler is WebRepositoryCrawler, "should be WebRepositoryCrawler");
                crawler.Dispose();
            }, token);

            await runner.RunTestAsync("CrawlerFactory: unsupported type throws NotSupportedException", async ct =>
            {
                CrawlPlan plan = new CrawlPlan { Name = "Test" };
                CrawlOperation op = new CrawlOperation();

                AssertHelper.ThrowsAsync<NotSupportedException>(async () =>
                {
                    CrawlerFactory.Create(
                        (RepositoryTypeEnum)999,
                        logging,
                        db,
                        plan,
                        op,
                        null, null, null,
                        System.IO.Path.GetTempPath(),
                        CancellationToken.None);
                }, "should throw for unsupported type");
            }, token);
        }
    }
}
