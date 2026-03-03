namespace Test.Integration.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Common;

    public static class PaginationTests
    {
        public static async Task RunAllAsync(TestServer server, TestRunner runner, CancellationToken token)
        {
            Console.WriteLine();
            Console.WriteLine("--- Pagination Tests ---");

            string tenantId = server.DefaultTenantId;

            // Create 3 assistants for pagination testing
            string[] assistantIds = new string[3];
            for (int i = 0; i < 3; i++)
            {
                var payload = new { Name = $"Pagination Asst {i}", TenantId = tenantId };
                string json = JsonSerializer.Serialize(payload);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage resp = await server.Client.PutAsync("/v1.0/assistants", content);
                string body = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("Id", out JsonElement idElem))
                    assistantIds[i] = idElem.GetString();
                else if (doc.RootElement.TryGetProperty("id", out JsonElement idElem2))
                    assistantIds[i] = idElem2.GetString();
            }

            await runner.RunTestAsync("Pagination.MaxResults1_MultiplePages", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=1");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp.StatusCode, "page 1 should return 200");

                string body = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(body);

                // Should have objects
                bool hasObjects = doc.RootElement.TryGetProperty("Objects", out JsonElement objsElem)
                    || doc.RootElement.TryGetProperty("objects", out objsElem);
                AssertHelper.IsTrue(hasObjects, "response should have Objects property");
                AssertHelper.AreEqual(1, objsElem.GetArrayLength(), "page 1 should have exactly 1 item");
            }, token);

            await runner.RunTestAsync("Pagination.ContinuationToken_Works", async ct =>
            {
                // Get page 1
                HttpResponseMessage resp1 = await server.Client.GetAsync("/v1.0/assistants?maxResults=1");
                string body1 = await resp1.Content.ReadAsStringAsync();
                using JsonDocument doc1 = JsonDocument.Parse(body1);

                string continuationToken = null;
                if (doc1.RootElement.TryGetProperty("ContinuationToken", out JsonElement ctElem))
                    continuationToken = ctElem.GetString();
                else if (doc1.RootElement.TryGetProperty("continuationToken", out JsonElement ctElem2))
                    continuationToken = ctElem2.GetString();

                AssertHelper.IsNotNull(continuationToken, "page 1 should have a continuation token");

                // Get page 2
                HttpResponseMessage resp2 = await server.Client.GetAsync($"/v1.0/assistants?maxResults=1&continuationToken={continuationToken}");
                AssertHelper.AreEqual((int)HttpStatusCode.OK, (int)resp2.StatusCode, "page 2 should return 200");

                string body2 = await resp2.Content.ReadAsStringAsync();
                using JsonDocument doc2 = JsonDocument.Parse(body2);

                bool hasObjects2 = doc2.RootElement.TryGetProperty("Objects", out JsonElement objs2)
                    || doc2.RootElement.TryGetProperty("objects", out objs2);
                AssertHelper.IsTrue(hasObjects2, "page 2 should have Objects");
                AssertHelper.AreEqual(1, objs2.GetArrayLength(), "page 2 should have exactly 1 item");

                // Pages should have different items
                string id1 = null, id2 = null;
                if (doc1.RootElement.TryGetProperty("Objects", out JsonElement o1) || doc1.RootElement.TryGetProperty("objects", out o1))
                {
                    var first = o1[0];
                    if (first.TryGetProperty("Id", out JsonElement ie)) id1 = ie.GetString();
                    else if (first.TryGetProperty("id", out JsonElement ie2)) id1 = ie2.GetString();
                }
                if (doc2.RootElement.TryGetProperty("Objects", out JsonElement o2) || doc2.RootElement.TryGetProperty("objects", out o2))
                {
                    var first = o2[0];
                    if (first.TryGetProperty("Id", out JsonElement ie)) id2 = ie.GetString();
                    else if (first.TryGetProperty("id", out JsonElement ie2)) id2 = ie2.GetString();
                }
                AssertHelper.AreNotEqual(id1, id2, "page 1 and page 2 should have different items");
            }, token);

            await runner.RunTestAsync("Pagination.EndOfResults_OnFinalPage", async ct =>
            {
                // Get all with large maxResults
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=100");
                string body = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(body);

                bool endOfResults = false;
                if (doc.RootElement.TryGetProperty("EndOfResults", out JsonElement eorElem))
                    endOfResults = eorElem.GetBoolean();
                else if (doc.RootElement.TryGetProperty("endOfResults", out JsonElement eorElem2))
                    endOfResults = eorElem2.GetBoolean();

                AssertHelper.IsTrue(endOfResults, "large page should indicate end of results");
            }, token);

            await runner.RunTestAsync("Pagination.TotalRecords_Accurate", async ct =>
            {
                HttpResponseMessage resp = await server.Client.GetAsync("/v1.0/assistants?maxResults=100");
                string body = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(body);

                int totalRecords = 0;
                if (doc.RootElement.TryGetProperty("TotalRecords", out JsonElement trElem))
                    totalRecords = trElem.GetInt32();
                else if (doc.RootElement.TryGetProperty("totalRecords", out JsonElement trElem2))
                    totalRecords = trElem2.GetInt32();

                AssertHelper.IsTrue(totalRecords >= 3, "total records should be at least 3, got " + totalRecords);
            }, token);

            // Cleanup
            foreach (string id in assistantIds)
            {
                if (id != null)
                    await server.Client.DeleteAsync($"/v1.0/assistants/{id}");
            }
        }
    }
}
