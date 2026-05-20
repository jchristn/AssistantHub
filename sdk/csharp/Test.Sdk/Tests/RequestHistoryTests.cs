namespace Test.Sdk.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk;
    using AssistantHub.Sdk.Models;
    using Test.Shared;

    /// <summary>
    /// Tests for request-history read operations.
    /// </summary>
    public static class RequestHistoryTests
    {
        /// <summary>
        /// Runs request-history coverage tests.
        /// </summary>
        /// <param name="runner">Test runner instance.</param>
        /// <param name="client">AssistantHub client.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RunAsync(TestRunner runner, AssistantHubClient client, CancellationToken token)
        {
            string capturedRequestId = null;
            DateTime startUtc = DateTime.UtcNow.AddSeconds(-5);

            await runner.RunTestAsync("RequestHistory: Capture and list whoami request", async (CancellationToken ct) =>
            {
                await client.WhoAmIAsync(ct).ConfigureAwait(false);

                for (int attempt = 0; attempt < 20; attempt++)
                {
                    EnumerationResult<RequestHistoryEntry> result = await client.ListRequestHistoryAsync(new RequestHistorySearchFilter
                    {
                        MaxResults = 25,
                        PathContains = "/v1.0/whoami",
                        StartUtc = startUtc
                    }, ct).ConfigureAwait(false);

                    AssertHelper.IsNotNull(result, "ListRequestHistory result");
                    AssertHelper.IsNotNull(result.Objects, "ListRequestHistory result.Objects");

                    RequestHistoryEntry entry = result.Objects.FirstOrDefault(item =>
                        !String.IsNullOrWhiteSpace(item.RequestPath)
                        && item.RequestPath.Contains("/v1.0/whoami"));

                    if (entry != null && !String.IsNullOrWhiteSpace(entry.Id))
                    {
                        capturedRequestId = entry.Id;
                        return;
                    }

                    await Task.Delay(500, ct).ConfigureAwait(false);
                }

                throw new Exception("Timed out waiting for request-history capture of /v1.0/whoami.");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("RequestHistory: Get request-history entry by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(capturedRequestId, "capturedRequestId from previous test");
                RequestHistoryEntry entry = await client.GetRequestHistoryAsync(capturedRequestId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(entry, "GetRequestHistory result");
                AssertHelper.AreEqual(capturedRequestId, entry.Id, "RequestHistory ID");
                AssertHelper.StringContains(entry.RequestPath, "/v1.0/whoami", "RequestHistory RequestPath");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("RequestHistory: Get detailed request-history entry by ID", async (CancellationToken ct) =>
            {
                AssertHelper.IsNotNull(capturedRequestId, "capturedRequestId from previous test");
                RequestHistoryEntry entry = await client.GetRequestHistoryDetailAsync(capturedRequestId, ct).ConfigureAwait(false);
                AssertHelper.IsNotNull(entry, "GetRequestHistoryDetail result");
                AssertHelper.AreEqual(capturedRequestId, entry.Id, "RequestHistory detail ID");
            }, token).ConfigureAwait(false);

            await runner.RunTestAsync("RequestHistory: Get request-history summary", async (CancellationToken ct) =>
            {
                RequestHistorySummaryResult summary = await client.GetRequestHistorySummaryAsync(new RequestHistorySearchFilter
                {
                    PathContains = "/v1.0/whoami",
                    StartUtc = startUtc,
                    BucketSeconds = 60
                }, ct).ConfigureAwait(false);

                AssertHelper.IsNotNull(summary, "GetRequestHistorySummary result");
                AssertHelper.IsGreaterThanOrEqual(summary.TotalCount, 1, "RequestHistory summary total count");
            }, token).ConfigureAwait(false);
        }
    }
}
