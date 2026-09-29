namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Services.Crawlers;
    using AssistantHub.Core.Settings;
    using AssistantHub.Server.Services;
    using Test.Shared;

    /// <summary>
    /// Tests for the retrieval and ingestion improvements: cross-encoder rerank, conversation and additive query
    /// rewrite, utility timeouts and circuit breakers, answer retry and regeneration, document supersession,
    /// provenance, structured cells, extraction settings, duplicate detection and crawler lookups.
    /// </summary>
    public partial class ServiceSuite
    {
        private async Task RunRetrievalImprovementTestsAsync()
        {
            #region Cross-encoder client

            await ExecuteTestAsync("CrossEncoderRerankClient.ParseScores: TEI results are returned in passage order", async () =>
            {
                List<double> scores = CrossEncoderRerankClient.ParseScores("[{\"index\":2,\"score\":0.9},{\"index\":0,\"score\":0.4},{\"index\":1,\"score\":0.1}]", 3, false);
                AssertHelper.HasCount(scores, 3, "score count");
                AssertHelper.AreEqual(0.4, scores[0], "passage 0 score");
                AssertHelper.AreEqual(0.1, scores[1], "passage 1 score");
                AssertHelper.AreEqual(0.9, scores[2], "passage 2 score");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("CrossEncoderRerankClient.ParseScores: Cohere results use relevance_score and unscored passages get 0", async () =>
            {
                List<double> scores = CrossEncoderRerankClient.ParseScores("{\"results\":[{\"index\":1,\"relevance_score\":0.75}]}", 2, true);
                AssertHelper.AreEqual(0.0, scores[0], "unscored passage");
                AssertHelper.AreEqual(0.75, scores[1], "scored passage");

                bool threw = false;
                try { CrossEncoderRerankClient.ParseScores("{\"results\":[]}", 2, true); }
                catch (InvalidOperationException) { threw = true; }
                AssertHelper.IsTrue(threw, "a response that scores nothing is an error");

                threw = false;
                try { CrossEncoderRerankClient.ParseScores("not json", 1, false); }
                catch (InvalidOperationException) { threw = true; }
                AssertHelper.IsTrue(threw, "invalid JSON is an error");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("CrossEncoderRerankClient.ScoreAsync: TEI and Cohere request shapes", async () =>
            {
                MockHttpMessageHandler handler = new MockHttpMessageHandler()
                    .When("/v1/rerank", HttpStatusCode.OK, "{\"results\":[{\"index\":0,\"relevance_score\":0.2},{\"index\":1,\"relevance_score\":0.8}]}")
                    .When("/rerank", HttpStatusCode.OK, "[{\"index\":0,\"score\":0.6},{\"index\":1,\"score\":0.3}]");
                using HttpClient httpClient = handler.CreateClient();
                CrossEncoderRerankClient client = new CrossEncoderRerankClient(httpClient);

                RerankerSettings tei = new RerankerSettings { Id = "tei", Endpoint = "http://reranker.test/", MaxPassageCharacters = 5 };
                AssertHelper.AreEqual(200, tei.MaxPassageCharacters, "passage limit clamped to at least 200");
                List<double> teiScores = await client.ScoreAsync(tei, "engine speed", new List<string> { new string('x', 250), "short" }).ConfigureAwait(false);
                AssertHelper.AreEqual(0.6, teiScores[0], "TEI score 0");
                AssertHelper.StringContains(handler.Requests[0].Url, "http://reranker.test/rerank", "TEI url");
                AssertHelper.StringContains(handler.Requests[0].Body, "\"texts\":[\"" + new string('x', 200) + "\",\"short\"]", "passages truncated to MaxPassageCharacters");
                AssertHelper.StringContains(handler.Requests[0].Body, "\"truncate\":true", "TEI truncates server-side");

                RerankerSettings cohere = new RerankerSettings { Id = "co", Format = "cohere", Endpoint = "https://api.cohere.test", Model = "rerank-v3.5", ApiKey = "secret" };
                List<double> cohereScores = await client.ScoreAsync(cohere, "engine speed", new List<string> { "a", "b" }).ConfigureAwait(false);
                AssertHelper.AreEqual("Cohere", cohere.Format, "format normalized");
                AssertHelper.AreEqual(0.8, cohereScores[1], "Cohere score 1");
                AssertHelper.StringContains(handler.Requests[1].Url, "https://api.cohere.test/v1/rerank", "Cohere url");
                AssertHelper.StringContains(handler.Requests[1].Body, "\"model\":\"rerank-v3.5\"", "Cohere model");
                AssertHelper.AreEqual("Bearer secret", handler.Requests[1].Headers.Authorization?.ToString(), "Cohere API key");
            });

            #endregion

            #region Utility helpers

            await ExecuteTestAsync("UtilityCircuitBreaker: opens after the failure threshold and closes on success or expiry", async () =>
            {
                UtilityCircuitBreaker.Reset();
                try
                {
                    AssertHelper.IsFalse(UtilityCircuitBreaker.RecordFailure("test:a", 2, 60000), "first failure does not open");
                    AssertHelper.IsFalse(UtilityCircuitBreaker.IsOpen("test:a"), "closed after one failure");
                    AssertHelper.IsTrue(UtilityCircuitBreaker.RecordFailure("test:a", 2, 60000), "second failure opens");
                    AssertHelper.IsTrue(UtilityCircuitBreaker.IsOpen("test:a"), "open after threshold");
                    AssertHelper.IsFalse(UtilityCircuitBreaker.IsOpen("test:b"), "keys are independent");

                    UtilityCircuitBreaker.RecordSuccess("test:a");
                    AssertHelper.IsFalse(UtilityCircuitBreaker.IsOpen("test:a"), "success closes");

                    UtilityCircuitBreaker.RecordFailure("test:c", 1, 1);
                    await Task.Delay(20).ConfigureAwait(false);
                    AssertHelper.IsFalse(UtilityCircuitBreaker.IsOpen("test:c"), "breaker closes when the open window expires");
                }
                finally
                {
                    UtilityCircuitBreaker.Reset();
                }
            });

            await ExecuteTestAsync("EmbeddingModelProfiles.Resolve: known model families get their task prefixes", async () =>
            {
                AssertHelper.AreEqual(("search_query: ", "search_document: "), EmbeddingModelProfiles.Resolve("nomic-embed-text:latest"), "nomic");
                AssertHelper.AreEqual(("query: ", "passage: "), EmbeddingModelProfiles.Resolve("intfloat/multilingual-e5-large"), "e5");
                AssertHelper.AreEqual("", EmbeddingModelProfiles.Resolve("mxbai-embed-large").DocumentPrefix, "mxbai has no document prefix");
                AssertHelper.StartsWith(EmbeddingModelProfiles.Resolve("bge-m3").QueryPrefix, "Represent", "bge query instruction");
                AssertHelper.AreEqual(("", ""), EmbeddingModelProfiles.Resolve("all-minilm"), "unknown model");
                AssertHelper.AreEqual(("", ""), EmbeddingModelProfiles.Resolve(null), "null model");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("RetrievalFusionHelper.FuseByReciprocalRank: a lower-weight list re-ranks without displacing the main query", async () =>
            {
                RetrievalChunk a = new RetrievalChunk { DocumentId = "adoc_a", Position = 0, Content = "a" };
                RetrievalChunk b = new RetrievalChunk { DocumentId = "adoc_b", Position = 0, Content = "b" };
                List<IReadOnlyList<RetrievalChunk>> lists = new List<IReadOnlyList<RetrievalChunk>>
                {
                    new List<RetrievalChunk> { a, b },
                    new List<RetrievalChunk> { b, a }
                };

                List<RetrievalChunk> weighted = RetrievalFusionHelper.FuseByReciprocalRank(lists, 10, 60.0, new List<double> { 1.0, 0.5 });
                AssertHelper.AreEqual("adoc_a", weighted[0].DocumentId, "main query's top result stays first");

                List<RetrievalChunk> reversed = RetrievalFusionHelper.FuseByReciprocalRank(lists, 10, 60.0, new List<double> { 0.5, 1.0 });
                AssertHelper.AreEqual("adoc_b", reversed[0].DocumentId, "weights decide the order");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("AssistantChatService.ParseConversationRewrite: JSON, bare line, think blocks and empty replies", async () =>
            {
                AssertHelper.AreEqual("What is the torque for the 220?", AssistantChatService.ParseConversationRewrite("{\"query\": \"What is the torque for the 220?\"}"), "JSON reply");
                AssertHelper.AreEqual("torque of model 220", AssistantChatService.ParseConversationRewrite("<think>the user means the 220</think>\n{\"query\":\"torque of model 220\"}"), "think block removed");
                AssertHelper.AreEqual("torque of model 220", AssistantChatService.ParseConversationRewrite("Here you go: {\"query\":\"torque of model 220\"} done"), "JSON inside prose");
                AssertHelper.AreEqual("torque of model 220", AssistantChatService.ParseConversationRewrite("\"torque of model 220\"\nextra"), "bare first line");
                AssertHelper.IsNull(AssistantChatService.ParseConversationRewrite("{\"query\": \"\"}"), "empty query");
                AssertHelper.IsNull(AssistantChatService.ParseConversationRewrite("{\"other\": \"x\"}"), "missing query property");
                AssertHelper.IsNull(AssistantChatService.ParseConversationRewrite("   "), "blank reply");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("AssistantChatService.IsDegenerateCitedAnswer: detects citation-only and lead-in fragments", async () =>
            {
                AssertHelper.IsTrue(AssistantChatService.IsDegenerateCitedAnswer(""), "empty");
                AssertHelper.IsTrue(AssistantChatService.IsDegenerateCitedAnswer("[1] [2]"), "citation markers only");
                AssertHelper.IsTrue(AssistantChatService.IsDegenerateCitedAnswer("According to the sources"), "lead-in only");
                AssertHelper.IsTrue(AssistantChatService.IsDegenerateCitedAnswer("Based on the provided context:"), "lead-in with colon");
                AssertHelper.IsTrue(AssistantChatService.IsDegenerateCitedAnswer("Yes [1]."), "too short");
                AssertHelper.IsFalse(AssistantChatService.IsDegenerateCitedAnswer("The engine is rated at 3000 rpm [1]."), "real answer");
                await Task.CompletedTask;
            });

            #endregion

            #region Settings and configuration models

            await ExecuteTestAsync("AssistantSettings: retrieval defaults and validation for new settings", async () =>
            {
                AssistantSettings s = new AssistantSettings();
                AssertHelper.AreEqual("Hybrid", s.SearchMode, "SearchMode default");
                AssertHelper.AreEqual(0.5, s.TextWeight, "TextWeight default");
                AssertHelper.AreEqual(1, s.RetrievalIncludeNeighbors, "RetrievalIncludeNeighbors default");
                AssertHelper.AreEqual("Llm", s.RerankerType, "RerankerType default");
                AssertHelper.AreEqual(20, s.RerankCandidateCount, "RerankCandidateCount default");
                AssertHelper.IsNull(s.RerankMinScore, "RerankMinScore default");
                AssertHelper.AreEqual("Demote", s.SupersessionMode, "SupersessionMode default");
                AssertHelper.IsFalse(s.EnableConversationRewrite, "conversation rewrite off by default");
                AssertHelper.IsFalse(s.EmbeddingTaskPrefixes, "task prefixes off by default");

                s.RerankerType = "crossencoder";
                AssertHelper.AreEqual("CrossEncoder", s.RerankerType, "RerankerType normalized");
                s.SupersessionMode = "hide";
                AssertHelper.AreEqual("Hide", s.SupersessionMode, "SupersessionMode normalized");

                AssertHelper.ThrowsAsync<ArgumentException>(() => { s.RerankerType = "Bm25"; return Task.CompletedTask; }, "unknown RerankerType");
                AssertHelper.ThrowsAsync<ArgumentException>(() => { s.SupersessionMode = "Delete"; return Task.CompletedTask; }, "unknown SupersessionMode");
                AssertHelper.ThrowsAsync<ArgumentOutOfRangeException>(() => { s.RerankCandidateCount = 0; return Task.CompletedTask; }, "RerankCandidateCount below 1");
                AssertHelper.ThrowsAsync<ArgumentOutOfRangeException>(() => { s.RerankCandidateCount = 201; return Task.CompletedTask; }, "RerankCandidateCount above 200");
                AssertHelper.ThrowsAsync<ArgumentOutOfRangeException>(() => { s.RerankMinScore = 1.5; return Task.CompletedTask; }, "RerankMinScore above 1");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("IngestionChunkingConfig and IngestionExtractionConfig: defaults, normalization and DocumentAtom settings", async () =>
            {
                IngestionChunkingConfig chunking = new IngestionChunkingConfig();
                AssertHelper.AreEqual("ParagraphBased", chunking.Strategy, "default strategy");
                AssertHelper.AreEqual("Flat", chunking.CellMode, "default cell mode");
                AssertHelper.AreEqual("RowGroupWithHeaders", chunking.TableStrategy, "default table strategy");
                AssertHelper.AreEqual("WholeList", chunking.ListStrategy, "default list strategy");
                AssertHelper.AreEqual("None", chunking.ContextHeader, "default context header");
                chunking.CellMode = "structured";
                chunking.ContextHeader = "titleandheadings";
                AssertHelper.AreEqual("Structured", chunking.CellMode, "cell mode normalized");
                AssertHelper.AreEqual("TitleAndHeadings", chunking.ContextHeader, "context header normalized");
                AssertHelper.ThrowsAsync<ArgumentOutOfRangeException>(() => { chunking.TableStrategy = "Columns"; return Task.CompletedTask; }, "unknown table strategy");

                IngestionExtractionConfig extraction = new IngestionExtractionConfig();
                AssertHelper.AreEqual("Allow", extraction.DuplicatePolicy, "default duplicate policy");
                AssertHelper.AreEqual(0.85, extraction.NearDuplicateThreshold, "default near-duplicate threshold");
                AssertHelper.IsNull(extraction.ToDocumentAtomSettings("pdf"), "no settings by default");

                Dictionary<string, object> tsv = extraction.ToDocumentAtomSettings("tsv");
                AssertHelper.AreEqual("\t", tsv["ColumnDelimiter"], "tsv delimiter");

                extraction.OcrEmbeddedImages = true;
                extraction.CsvHasHeaderRow = false;
                extraction.CsvRowsPerAtom = 50;
                extraction.ExcelHeaderRowScoreThreshold = 4;
                extraction.DuplicatePolicy = "reject";
                Dictionary<string, object> atom = extraction.ToDocumentAtomSettings("csv");
                AssertHelper.AreEqual(true, atom["ExtractAtomsFromImages"], "OCR setting");
                AssertHelper.AreEqual(false, atom["HasHeaderRow"], "header row setting");
                AssertHelper.AreEqual(50, atom["RowsPerAtom"], "rows per atom");
                AssertHelper.AreEqual(4, atom["HeaderRowScoreThreshold"], "header row threshold");
                AssertHelper.AreEqual("Reject", extraction.DuplicatePolicy, "duplicate policy normalized");
                AssertHelper.ThrowsAsync<ArgumentOutOfRangeException>(() => { extraction.NearDuplicateThreshold = 1.2; return Task.CompletedTask; }, "threshold above 1");

                IngestionRule rule = new IngestionRule { Extraction = extraction };
                string json = JsonSerializer.Serialize(rule);
                IngestionRule roundTrip = JsonSerializer.Deserialize<IngestionRule>(json);
                AssertHelper.AreEqual("Reject", roundTrip.Extraction.DuplicatePolicy, "extraction round-trips through JSON");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("DocumentAtomAtomizationService: route map supports tsv and rejects legacy Office formats", async () =>
            {
                AssertHelper.AreEqual("/atom/csv", DocumentAtomAtomizationService.GetAtomPath("tsv"), "tsv uses the csv route");
                AssertHelper.AreEqual("/atom/pdf", DocumentAtomAtomizationService.GetAtomPath("pdf"), "pdf route");
                AssertHelper.IsNull(DocumentAtomAtomizationService.GetAtomPath("doc"), "no route for legacy doc");
                AssertHelper.IsTrue(DocumentAtomAtomizationService.IsLegacyBinaryOfficeType("xls"), "xls is legacy");
                AssertHelper.IsFalse(DocumentAtomAtomizationService.IsLegacyBinaryOfficeType("xlsx"), "xlsx is not legacy");

                DocumentAtomAtomizationService service = new DocumentAtomAtomizationService(
                    new DocumentAtomSettings { Endpoint = "http://127.0.0.1:9" },
                    CreateSilentLogging());
                AtomExtractionResult result = await service.ExtractAsync("adoc_legacy", Encoding.UTF8.GetBytes("x"), "ppt", "deck.ppt", null).ConfigureAwait(false);
                AssertHelper.IsNotNull(result, "legacy result");
                AssertHelper.StringContains(result.ErrorMessage, "not supported", "legacy error message");
            });

            await ExecuteTestAsync("DocumentAtomAtomizationService.ExtractAsync: returns structured blocks with pages, tables and lists", async () =>
            {
                string atomJson =
                    "[" +
                    "{\"Type\":\"Text\",\"Text\":\"Specifications\",\"HeaderLevel\":1,\"PageNumber\":2}," +
                    "{\"Type\":\"Text\",\"Text\":\"The engine is rated at 3000 rpm.\",\"PageNumber\":2}," +
                    "{\"Type\":\"Table\",\"PageNumber\":3,\"Table\":{\"Columns\":[{\"Name\":\"Model\"},{\"Name\":\"Torque\"}],\"Rows\":[{\"Model\":\"220\",\"Torque\":\"40\"}]}}," +
                    "{\"Type\":\"List\",\"PageNumber\":3,\"OrderedList\":[\"Open the valve\",\"Start the pump\"]}" +
                    "]";

                using (PathStubServer stub = new PathStubServer(GetAvailableTcpPort()))
                {
                    stub.Map("/atom/csv", HttpStatusCode.OK, atomJson);
                    stub.Start();
                    DocumentAtomAtomizationService service = new DocumentAtomAtomizationService(
                        new DocumentAtomSettings { Endpoint = stub.BaseUrl },
                        CreateSilentLogging());

                    AtomExtractionResult result = await service.ExtractAsync(
                        "adoc_blocks", Encoding.UTF8.GetBytes("a\tb"), "tsv", "specs.tsv", new IngestionExtractionConfig { CsvHasHeaderRow = true }).ConfigureAwait(false);

                    AssertHelper.IsNotNull(result, "extraction result");
                    AssertHelper.HasCount(result.Blocks, 4, "block count");
                    AssertHelper.AreEqual("Heading", result.Blocks[0].Kind, "heading block");
                    AssertHelper.AreEqual(2, result.Blocks[1].PageNumber, "text block page");
                    AssertHelper.AreEqual("Table", result.Blocks[2].Kind, "table block");
                    AssertHelper.AreEqual("Model", result.Blocks[2].TableRows[0][0], "table header first");
                    AssertHelper.AreEqual("40", result.Blocks[2].TableRows[1][1], "table cell");
                    AssertHelper.AreEqual("List", result.Blocks[3].Kind, "list block");
                    AssertHelper.IsTrue(result.Blocks[3].Ordered, "ordered list");
                    AssertHelper.StringContains(result.Text, "3000 rpm", "flat text still produced");
                    AssertHelper.StringContains(stub.Bodies[0], "\"ColumnDelimiter\":\"\\t\"", "tsv delimiter sent");
                    AssertHelper.StringContains(stub.Bodies[0], "\"HasHeaderRow\":true", "extraction settings sent");
                }
            });

            #endregion

            #region Structured cells and provenance

            await ExecuteTestAsync("StructuredCellBuilder.Build: sections, tables and lists become cells with headings and pages", async () =>
            {
                List<ExtractedBlock> blocks = new List<ExtractedBlock>
                {
                    new ExtractedBlock { Kind = "Heading", Text = "Setup", HeaderLevel = 1, PageNumber = 1 },
                    new ExtractedBlock { Kind = "Text", Text = "Unpack the unit.", PageNumber = 1 },
                    new ExtractedBlock { Kind = "Heading", Text = "Networking", HeaderLevel = 2, PageNumber = 2 },
                    new ExtractedBlock { Kind = "Text", Text = "Connect port A.", PageNumber = 2 },
                    new ExtractedBlock { Kind = "Text", Text = "Then port B.", PageNumber = 3 },
                    new ExtractedBlock { Kind = "Table", TableRows = new List<List<string>> { new List<string> { "Port", "Speed" }, new List<string> { "A", "1G" } }, PageNumber = 3 },
                    new ExtractedBlock { Kind = "List", ListItems = new List<string> { "one", "two" }, Ordered = false, PageNumber = 4 },
                    new ExtractedBlock { Kind = "Heading", Text = "Safety", HeaderLevel = 1, PageNumber = 5 },
                    new ExtractedBlock { Kind = "Text", Text = "Wear gloves.", PageNumber = 5 }
                };

                IngestionChunkingConfig chunking = new IngestionChunkingConfig { CellMode = "Structured", ContextHeader = "TitleAndHeadings", ListStrategy = "ListEntry" };
                List<StructuredCell> cells = StructuredCellBuilder.Build(blocks, "Pump Manual", chunking, "search_document: ");

                AssertHelper.HasCount(cells, 5, "cell count");
                AssertHelper.AreEqual("Text", cells[0].Type, "first section");
                AssertHelper.StringContains(cells[0].Text, "# Setup", "heading kept with its section");
                AssertHelper.AreEqual("Setup", cells[0].Section, "first section path");
                AssertHelper.AreEqual("Setup > Networking", cells[1].Section, "nested section path");
                AssertHelper.AreEqual(2, cells[1].PageStart, "section page start");
                AssertHelper.AreEqual(3, cells[1].PageEnd, "section page end");
                AssertHelper.AreEqual("search_document: Pump Manual > Setup > Networking\n", cells[1].ContextPrefix, "prefix and context header");
                AssertHelper.AreEqual("Table", cells[2].Type, "table cell");
                AssertHelper.AreEqual("RowGroupWithHeaders", cells[2].Strategy, "table strategy");
                AssertHelper.AreEqual("List", cells[3].Type, "list cell");
                AssertHelper.AreEqual("ListEntry", cells[3].Strategy, "list strategy");
                AssertHelper.AreEqual("Safety", cells[4].Section, "a new top-level heading resets the path");

                Dictionary<string, string> tags = cells[1].ProvenanceTagValues();
                AssertHelper.AreEqual("00002", tags[ProvenanceTags.PageStart], "page tag zero-padded");
                AssertHelper.AreEqual("Setup > Networking", tags[ProvenanceTags.Section], "section tag");

                AssertHelper.HasCount(StructuredCellBuilder.Build(new List<ExtractedBlock>(), "t", chunking, ""), 0, "no blocks, no cells");
                AssertHelper.AreEqual("", StructuredCellBuilder.BuildHeader("None", "Title", "S", 100), "no header");
                AssertHelper.AreEqual("Title\n", StructuredCellBuilder.BuildHeader("Title", "Title", "S", 100), "title header ignores section");
                AssertHelper.AreEqual("A long\n", StructuredCellBuilder.BuildHeader("Title", "A long document title", null, 8), "header cut at a word boundary");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("ProvenanceTags.Apply and RetrievalChunk.ProvenanceLabel: pages and sheets reach the prompt label", async () =>
            {
                RetrievalChunk chunk = new RetrievalChunk();
                ProvenanceTags.Apply(chunk, new Dictionary<string, string>
                {
                    [ProvenanceTags.PageStart] = "00003",
                    [ProvenanceTags.PageEnd] = "00004",
                    [ProvenanceTags.Sheet] = "Q1",
                    [ProvenanceTags.Section] = "Results"
                });
                AssertHelper.AreEqual(3, chunk.PageStart, "page start");
                AssertHelper.AreEqual(4, chunk.PageEnd, "page end");
                AssertHelper.AreEqual("pp. 3-4, sheet Q1", chunk.ProvenanceLabel(), "range label");

                RetrievalChunk single = new RetrievalChunk();
                ProvenanceTags.Apply(single, new Dictionary<string, string> { [ProvenanceTags.PageStart] = "00007", [ProvenanceTags.Sheet] = "" });
                AssertHelper.AreEqual(7, single.PageEnd, "page end defaults to page start");
                AssertHelper.AreEqual("p. 7", single.ProvenanceLabel(), "single page label");

                RetrievalChunk none = new RetrievalChunk();
                ProvenanceTags.Apply(none, new Dictionary<string, string> { [ProvenanceTags.PageStart] = "x" });
                AssertHelper.IsNull(none.ProvenanceLabel(), "no provenance, no label");
                AssertHelper.AreEqual(3, chunk.CloneForSnapshot().PageStart, "snapshot keeps provenance");
                await Task.CompletedTask;
            });

            #endregion

            #region Duplicates and supersession

            await ExecuteTestAsync("IngestionServiceBase: content hash, chunk record parsing and duplicate merging", async () =>
            {
                AssertHelper.AreEqual(
                    "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
                    IngestionServiceBase.ComputeContentSha256(Encoding.UTF8.GetBytes("hello")),
                    "sha256 hex");
                AssertHelper.HasCount(IngestionServiceBase.ParseChunkRecordIds("[\"a\",\"\",\"b\"]"), 2, "chunk ids without blanks");
                AssertHelper.HasCount(IngestionServiceBase.ParseChunkRecordIds("not json"), 0, "invalid chunk ids");

                string merged = IngestionServiceBase.MergeExactDuplicates(
                    "[{\"DocumentId\":\"adoc_1\",\"Score\":1.0,\"Exact\":true}]",
                    "[{\"DocumentId\":\"adoc_1\",\"Score\":0.97},{\"DocumentId\":\"adoc_2\",\"Score\":0.9}]");
                using JsonDocument doc = JsonDocument.Parse(merged);
                AssertHelper.AreEqual(2, doc.RootElement.GetArrayLength(), "one entry per document");
                AssertHelper.IsTrue(doc.RootElement[0].GetProperty("Exact").GetBoolean(), "exact match wins");
                AssertHelper.IsNull(IngestionServiceBase.MergeExactDuplicates(null, "garbage"), "nothing to merge");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("DocumentSupersession: validate, set, replace and release links", async () =>
            {
                MockDatabaseDriver database = new MockDatabaseDriver();
                AssistantDocument v1 = CreateToolDocument("adoc_v1", "tenant_s", "col_s", "Policy v1", DocumentStatusEnum.Completed);
                AssistantDocument v2 = CreateToolDocument("adoc_v2", "tenant_s", "col_s", "Policy v2", DocumentStatusEnum.Completed);
                AssistantDocument v3 = CreateToolDocument("adoc_v3", "tenant_s", "col_s", "Policy v3", DocumentStatusEnum.Completed);
                AssistantDocument foreign = CreateToolDocument("adoc_foreign", "tenant_other", "col_s", "Other", DocumentStatusEnum.Completed);
                foreach (AssistantDocument d in new[] { v1, v2, v3, foreign })
                    await database.AssistantDocument.CreateAsync(d).ConfigureAwait(false);

                AssertHelper.IsNotNull(await DocumentSupersession.ValidateAsync(database, v2, new[] { "adoc_v2" }).ConfigureAwait(false), "cannot supersede itself");
                AssertHelper.IsNotNull(await DocumentSupersession.ValidateAsync(database, v2, new[] { "adoc_foreign" }).ConfigureAwait(false), "cannot supersede another tenant's document");
                AssertHelper.IsNotNull(await DocumentSupersession.ValidateAsync(database, v2, new[] { "adoc_missing" }).ConfigureAwait(false), "cannot supersede a missing document");
                AssertHelper.IsNull(await DocumentSupersession.ValidateAsync(database, v2, new[] { "adoc_v1" }).ConfigureAwait(false), "valid target");

                await DocumentSupersession.SetAsync(database, v2, new List<string> { "adoc_v1" }).ConfigureAwait(false);
                AssertHelper.AreEqual("adoc_v2", (await database.AssistantDocument.ReadAsync("adoc_v1").ConfigureAwait(false)).SupersededBy, "v1 superseded by v2");
                AssertHelper.HasCount(DocumentSupersession.ParseIds((await database.AssistantDocument.ReadAsync("adoc_v2").ConfigureAwait(false)).Supersedes), 1, "v2 supersedes one document");

                AssistantDocument v2Current = await database.AssistantDocument.ReadAsync("adoc_v2").ConfigureAwait(false);
                AssertHelper.IsNotNull(await DocumentSupersession.ValidateAsync(database, await database.AssistantDocument.ReadAsync("adoc_v1").ConfigureAwait(false), new[] { "adoc_v2" }).ConfigureAwait(false), "no two-document cycle");

                await DocumentSupersession.SetAsync(database, v3, new List<string> { "adoc_v2" }).ConfigureAwait(false);
                await DocumentSupersession.SetAsync(database, v2Current, new List<string>()).ConfigureAwait(false);
                AssertHelper.IsNull((await database.AssistantDocument.ReadAsync("adoc_v1").ConfigureAwait(false)).SupersededBy, "clearing releases v1");
                AssertHelper.IsNull((await database.AssistantDocument.ReadAsync("adoc_v2").ConfigureAwait(false)).Supersedes, "v2 supersedes nothing");
                AssertHelper.AreEqual("adoc_v3", (await database.AssistantDocument.ReadAsync("adoc_v2").ConfigureAwait(false)).SupersededBy, "clearing keeps v2's own replacement");

                await DocumentSupersession.ReleaseAsync(database, await database.AssistantDocument.ReadAsync("adoc_v3").ConfigureAwait(false)).ConfigureAwait(false);
                AssertHelper.IsNull((await database.AssistantDocument.ReadAsync("adoc_v2").ConfigureAwait(false)).SupersededBy, "deleting v3 makes v2 current");

                await DocumentSupersession.SetAsync(database, v3, new List<string> { "adoc_v2" }).ConfigureAwait(false);
                await DocumentSupersession.ReleaseAsync(database, await database.AssistantDocument.ReadAsync("adoc_v2").ConfigureAwait(false)).ConfigureAwait(false);
                AssertHelper.IsNull((await database.AssistantDocument.ReadAsync("adoc_v3").ConfigureAwait(false)).Supersedes, "deleting v2 removes it from v3's list");
            });

            await ExecuteTestAsync("Retrieval supersession: Demote replaces the outdated chunk with the newest version in the chain", async () =>
            {
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockDatabaseDriver database) =
                    await CreateSupersessionFixtureAsync("Demote").ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[" +
                    "{\"DocumentId\":\"adoc_old\",\"Score\":0.9,\"Content\":\"limit is 10\",\"Position\":0}," +
                    "{\"DocumentId\":\"adoc_other\",\"Score\":0.5,\"Content\":\"unrelated\",\"Position\":0}]}");
                vectorStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[{\"DocumentId\":\"adoc_newest\",\"Score\":0.4,\"Content\":\"limit is 30\",\"Position\":2}]}");

                AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "what is the limit?" }).ConfigureAwait(false);

                AssertHelper.IsTrue(result.Success, "retrieve success");
                AssertHelper.AreEqual(1, result.Response.SupersededChunks, "one superseded chunk");
                AssertHelper.HasCount(result.Response.Chunks, 2, "chunk count");
                AssertHelper.AreEqual("adoc_newest", result.Response.Chunks[0].DocumentId, "newest version takes the outdated chunk's place");
                AssertHelper.AreEqual("adoc_other", result.Response.Chunks[1].DocumentId, "other chunks keep their order");
                AssertHelper.StringContains(vectorStore.Calls[1].Body, "adoc_newest", "scoped search for the replacement");
                AssertHelper.IsTrue(result.Response.Stages.Any(s => s.Stage == "supersession"), "supersession stage captured");
            });

            await ExecuteTestAsync("Retrieval supersession: Hide drops and Include marks outdated chunks", async () =>
            {
                (AssistantChatService hideService, Assistant assistant, RecordingVectorStoreService hideStore, MockDatabaseDriver hideDatabase) =
                    await CreateSupersessionFixtureAsync("Hide").ConfigureAwait(false);
                hideStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[{\"DocumentId\":\"adoc_old\",\"Score\":0.9,\"Content\":\"limit is 10\",\"Position\":0}]}");
                AssistantRetrievalExecutionResult hidden = await hideService.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "limit?" }).ConfigureAwait(false);
                AssertHelper.HasCount(hidden.Response.Chunks, 0, "Hide removes superseded chunks");
                AssertHelper.HasCount(hideStore.Calls, 1, "Hide does not search for the replacement");

                (AssistantChatService includeService, Assistant includeAssistant, RecordingVectorStoreService includeStore, MockDatabaseDriver includeDatabase) =
                    await CreateSupersessionFixtureAsync("Include").ConfigureAwait(false);
                includeStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[{\"DocumentId\":\"adoc_old\",\"Score\":0.9,\"Content\":\"limit is 10\",\"Position\":0}]}");
                AssistantSettings settings = await includeDatabase.AssistantSettings.ReadByAssistantIdAsync(includeAssistant.Id).ConfigureAwait(false);
                settings.EnableCitations = true;
                AssistantPromptContext context = await includeService.BuildPromptContextAsync(
                    includeAssistant, settings, new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "limit?" } },
                    "limit?", null, null, null).ConfigureAwait(false);
                AssertHelper.HasCount(context.Chunks, 1, "Include keeps the chunk");
                AssertHelper.AreEqual("adoc_newest", context.Chunks[0].SupersededBy, "chunk points at the newest version");
                AssertHelper.StartsWith(context.ContextChunks[0], "[outdated: superseded by \"Newest\"]", "prompt marks outdated content");
                AssertHelper.StringContains(context.ChunkLabels[0], "outdated", "citation label marks outdated");
                AssertHelper.AreEqual("adoc_newest", context.CitationSources[0].SupersededBy, "citation carries the replacement");
            });

            await ExecuteTestAsync("Retrieval supersession: an explicit scope keeps the older version, marked outdated", async () =>
            {
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockDatabaseDriver database) =
                    await CreateSupersessionFixtureAsync("Demote").ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[{\"DocumentId\":\"adoc_old\",\"Score\":0.9,\"Content\":\"limit is 10\",\"Position\":0}]}");
                AssistantRetrievalExecutionResult attached = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest
                {
                    Query = "Under this version, what is the limit?",
                    AttachedDocumentIds = new List<string> { "adoc_old" }
                }).ConfigureAwait(false);
                AssertHelper.IsTrue(attached.Success, "retrieve success: " + attached.ErrorMessage);
                AssertHelper.HasCount(attached.Response.Chunks, 1, "attached old version kept");
                AssertHelper.AreEqual("adoc_old", attached.Response.Chunks[0].DocumentId, "old version returned");
                AssertHelper.AreEqual("adoc_newest", attached.Response.Chunks[0].SupersededBy, "marked outdated");
                AssertHelper.HasCount(vectorStore.Calls, 1, "no replacement search outside the attached scope");

                // A metadata filter that excludes the replacement also keeps the older version.
                vectorStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[{\"DocumentId\":\"adoc_old\",\"Score\":0.9,\"Content\":\"limit is 10\",\"Position\":0}]}");
                vectorStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[]}");
                AssistantRetrievalExecutionResult filteredResult = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest
                {
                    Query = "old limit",
                    MetadataFilter = new ChatMetadataFilter { RequiredLabels = new List<string> { "superseded" } }
                }).ConfigureAwait(false);
                AssertHelper.HasCount(filteredResult.Response.Chunks, 1, "filtered old version kept");
                AssertHelper.AreEqual("adoc_old", filteredResult.Response.Chunks[0].DocumentId, "old version returned under the filter");
            });

            #endregion

            #region Retrieval stages

            await ExecuteTestAsync("Retrieval: cross-encoder rerank uses a candidate pool, reorders, and keeps RerankerTopK", async () =>
            {
                UtilityCircuitBreaker.Reset();
                MockHttpMessageHandler tei = new MockHttpMessageHandler()
                    .When("/rerank", HttpStatusCode.OK, "[{\"index\":0,\"score\":0.1},{\"index\":1,\"score\":0.95},{\"index\":2,\"score\":0.5}]");
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s =>
                    {
                        s.EnableReranking = true;
                        s.RerankerType = "CrossEncoder";
                        s.RerankEndpointId = "xenc";
                        s.RetrievalTopK = 2;
                        s.RerankCandidateCount = 25;
                        s.RerankerTopK = 2;
                    }, tei).ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());

                AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "rated speed" }).ConfigureAwait(false);

                AssertHelper.IsTrue(result.Success, "retrieve success");
                AssertHelper.StringContains(vectorStore.Calls[0].Body, "\"MaxResults\":25", "candidate pool retrieved for the reranker");
                AssertHelper.AreEqual("cross_encoder", result.Response.Reranker, "reranker reported");
                AssertHelper.HasCount(result.Response.Chunks, 2, "RerankerTopK kept");
                AssertHelper.AreEqual("adoc_b", result.Response.Chunks[0].DocumentId, "best cross-encoder score first");
                AssertHelper.AreEqual(0.95, result.Response.Chunks[0].RerankScore, "rerank score recorded");
                AssertHelper.AreEqual(0, chat.Requests.Count, "no LLM call for cross-encoder rerank");
                AssertHelper.IsFalse(result.Response.NoRelevantContext, "relevant context found");
            });

            await ExecuteTestAsync("Retrieval: cross-encoder minimum score reports no relevant context", async () =>
            {
                UtilityCircuitBreaker.Reset();
                MockHttpMessageHandler tei = new MockHttpMessageHandler()
                    .When("/rerank", HttpStatusCode.OK, "[{\"index\":0,\"score\":0.01},{\"index\":1,\"score\":0.02},{\"index\":2,\"score\":0.03}]");
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s =>
                    {
                        s.EnableReranking = true;
                        s.RerankerType = "CrossEncoder";
                        s.RerankEndpointId = "xenc";
                        s.RerankMinScore = 0.3;
                    }, tei).ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());

                AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "unanswerable" }).ConfigureAwait(false);
                AssertHelper.IsTrue(result.Response.NoRelevantContext, "no relevant context flag");
                AssertHelper.HasCount(result.Response.Chunks, 0, "chunks below the minimum are dropped");
            });

            await ExecuteTestAsync("Retrieval: cross-encoder failures keep retrieval order and open the circuit breaker", async () =>
            {
                UtilityCircuitBreaker.Reset();
                try
                {
                    MockHttpMessageHandler tei = new MockHttpMessageHandler().When("/rerank", HttpStatusCode.ServiceUnavailable, "{}");
                    (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                        await CreateRetrievalFixtureAsync(s =>
                        {
                            s.EnableReranking = true;
                            s.RerankerType = "CrossEncoder";
                            s.RerankEndpointId = "xenc";
                            s.RetrievalTopK = 2;
                        }, tei).ConfigureAwait(false);

                    for (int i = 0; i < 3; i++)
                    {
                        vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());
                        AssistantRetrievalExecutionResult failed = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "q" }).ConfigureAwait(false);
                        AssertHelper.IsTrue(failed.Success, "retrieval still succeeds");
                        AssertHelper.AreEqual("adoc_a", failed.Response.Chunks[0].DocumentId, "retrieval order kept");
                        AssertHelper.HasCount(failed.Response.Chunks, 2, "unreranked candidates cut to RetrievalTopK");
                        AssertHelper.IsFalse(failed.Response.RerankSkipped, "failure is not a skip");
                    }

                    vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());
                    AssistantRetrievalExecutionResult skipped = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "q" }).ConfigureAwait(false);
                    AssertHelper.IsTrue(skipped.Response.RerankSkipped, "breaker open skips the reranker");
                    AssertHelper.AreEqual(3, tei.Requests.Count, "no call while the breaker is open");
                }
                finally
                {
                    UtilityCircuitBreaker.Reset();
                }
            });

            await ExecuteTestAsync("Retrieval: conversation rewrite searches the standalone question alongside the original", async () =>
            {
                UtilityCircuitBreaker.Reset();
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s => s.EnableConversationRewrite = true, null,
                        _ => ChatReply("{\"query\":\"What is the torque of the model 220?\"}")).ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());
                vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());

                AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest
                {
                    Messages = new List<ChatCompletionMessage>
                    {
                        new ChatCompletionMessage { Role = "user", Content = "What is the torque of the model 110?" },
                        new ChatCompletionMessage { Role = "assistant", Content = "40 Nm." },
                        new ChatCompletionMessage { Role = "user", Content = "and for the 220?" }
                    }
                }).ConfigureAwait(false);

                AssertHelper.IsTrue(result.Success, "retrieve success");
                AssertHelper.AreEqual("What is the torque of the model 220?", result.Response.ConversationRewrite, "standalone question");
                AssertHelper.HasCount(result.Response.Queries, 2, "original and standalone searched");
                AssertHelper.AreEqual("and for the 220?", result.Response.Queries[0], "original first");
                AssertHelper.StringContains(chat.Requests[0].Body, "40 Nm.", "rewrite prompt includes the conversation");

                // A first turn has no conversation to resolve, so no rewrite call is made.
                vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());
                AssistantRetrievalExecutionResult first = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "What is the torque?" }).ConfigureAwait(false);
                AssertHelper.IsNull(first.Response.ConversationRewrite, "no rewrite on the first turn");
                AssertHelper.AreEqual(1, chat.Requests.Count, "no extra model call");
            });

            await ExecuteTestAsync("Retrieval: query rewrite is additive and deterministic", async () =>
            {
                UtilityCircuitBreaker.Reset();
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s => s.EnableQueryRewrite = true, null,
                        _ => ChatReply("engine rated speed\nRPM rating of the engine\nengine rated speed\nmax rpm\nidle rpm")).ConfigureAwait(false);
                for (int i = 0; i < 4; i++) vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());

                AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "engine rated speed" }).ConfigureAwait(false);

                AssertHelper.HasCount(result.Response.Queries, 4, "original plus at most three variants");
                AssertHelper.AreEqual("engine rated speed", result.Response.Queries[0], "original query kept first");
                AssertHelper.AreEqual(1, result.Response.Queries.Count(q => q == "engine rated speed"), "duplicates of the original removed");
                AssertHelper.StringContains(chat.Requests[0].Body, "\"temperature\":0", "rewrite runs at temperature 0");
            });

            await ExecuteTestAsync("Retrieval: failing utility steps open the circuit breaker and fall back to the original query", async () =>
            {
                UtilityCircuitBreaker.Reset();
                try
                {
                    (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                        await CreateRetrievalFixtureAsync(s => s.EnableQueryRewrite = true, null,
                            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{\"error\":\"boom\"}", Encoding.UTF8, "application/json") }).ConfigureAwait(false);

                    int callsBefore = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());
                        callsBefore = chat.Requests.Count;
                        AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest { Query = "q" }).ConfigureAwait(false);
                        AssertHelper.IsTrue(result.Success, "retrieval succeeds without the rewrite");
                        AssertHelper.HasCount(result.Response.Queries, 1, "original query only");
                    }

                    AssertHelper.AreEqual(callsBefore, chat.Requests.Count, "no rewrite call once the breaker is open");
                }
                finally
                {
                    UtilityCircuitBreaker.Reset();
                }
            });

            await ExecuteTestAsync("Retrieval: settings_override runs retrieval with unsaved settings", async () =>
            {
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s => s.RetrievalTopK = 3).ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());

                AssistantSettings trial = CreateToolSettings(new AssistantToolPolicy());
                trial.EnableRag = true;
                trial.SearchMode = "Vector";
                trial.RetrievalTopK = 1;
                trial.RetrievalIncludeNeighbors = 0;
                AssistantRetrievalExecutionResult result = await service.ExecuteRetrievalOnlyAsync(assistant.Id, new AssistantRetrieveRequest
                {
                    Query = "q",
                    SettingsOverride = trial
                }).ConfigureAwait(false);

                AssertHelper.IsTrue(result.Success, "retrieve success");
                AssertHelper.AreEqual("Vector", result.Response.SearchMode, "override search mode used");
                AssertHelper.HasCount(result.Response.Chunks, 1, "override top K used");
            });

            await ExecuteTestAsync("Retrieval: provenance tags from search results reach chunks, labels and citations", async () =>
            {
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s => s.EnableCitations = true).ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, "{\"Documents\":[{\"DocumentId\":\"adoc_a\",\"Score\":0.9,\"Content\":\"rated at 3000 rpm\",\"Position\":0," +
                    "\"Tags\":{\"ah_page_start\":\"00012\",\"ah_page_end\":\"00012\",\"ah_section\":\"Specifications\"}}]}");

                MockDatabaseDriver database = (MockDatabaseDriver)typeof(AssistantChatServiceBase)
                    .GetField("_Database", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
                AssistantSettings settings = await database.AssistantSettings.ReadByAssistantIdAsync(assistant.Id).ConfigureAwait(false);
                AssistantPromptContext context = await service.BuildPromptContextAsync(
                    assistant, settings, new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "speed?" } },
                    "speed?", null, null, null).ConfigureAwait(false);

                AssertHelper.AreEqual(12, context.Chunks[0].PageStart, "chunk page");
                AssertHelper.AreEqual("Specifications", context.Chunks[0].Section, "chunk section");
                AssertHelper.StringContains(context.ChunkLabels[0], "p. 12", "prompt label includes the page");
                AssertHelper.AreEqual(12, context.CitationSources[0].PageStart, "citation page");
            });

            #endregion

            #region Answer generation

            await ExecuteTestAsync("AssistantChatService.ExecuteNonStreamingAsync: retries the answer call on a transient 429", async () =>
            {
                int calls = 0;
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s => s.EnableRag = false, null, _ =>
                    {
                        calls++;
                        return calls == 1
                            ? new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("{\"error\":\"busy\"}", Encoding.UTF8, "application/json") }
                            : ChatReply("The answer after a retry.");
                    }).ConfigureAwait(false);

                AssistantChatExecutionResult result = await service.ExecuteNonStreamingAsync(new AssistantChatExecutionRequest
                {
                    AssistantId = assistant.Id,
                    Assistant = assistant,
                    AssistantSettings = await ReadFixtureSettingsAsync(service, assistant).ConfigureAwait(false),
                    Messages = new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "hello" } }
                }).ConfigureAwait(false);

                AssertHelper.IsTrue(result.Success, "chat succeeds after retry: " + result.ErrorMessage);
                AssertHelper.AreEqual(2, chat.Requests.Count, "one retry");
                AssertHelper.AreEqual("The answer after a retry.", result.Response.Choices[0].Message.Content, "retried answer returned");
            });

            await ExecuteTestAsync("AssistantChatService.ExecuteNonStreamingAsync: does not retry a non-transient failure", async () =>
            {
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s => s.EnableRag = false, null,
                        _ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"error\":\"bad\"}", Encoding.UTF8, "application/json") }).ConfigureAwait(false);

                AssistantChatExecutionResult result = await service.ExecuteNonStreamingAsync(new AssistantChatExecutionRequest
                {
                    AssistantId = assistant.Id,
                    Assistant = assistant,
                    AssistantSettings = await ReadFixtureSettingsAsync(service, assistant).ConfigureAwait(false),
                    Messages = new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "hello" } }
                }).ConfigureAwait(false);

                AssertHelper.IsFalse(result.Success, "chat fails");
                AssertHelper.AreEqual(1, chat.Requests.Count, "no retry for 400");
            });

            await ExecuteTestAsync("AssistantChatService.ExecuteNonStreamingAsync: regenerates a degenerate cited answer without citations", async () =>
            {
                int calls = 0;
                (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                    await CreateRetrievalFixtureAsync(s => s.EnableCitations = true, null, _ =>
                    {
                        calls++;
                        return ChatReply(calls == 1 ? "According to the sources [1]" : "The engine is rated at 3000 rpm.");
                    }).ConfigureAwait(false);
                vectorStore.Enqueue(HttpStatusCode.OK, ThreeChunkSearchResponse());

                AssistantChatExecutionResult result = await service.ExecuteNonStreamingAsync(new AssistantChatExecutionRequest
                {
                    AssistantId = assistant.Id,
                    Assistant = assistant,
                    AssistantSettings = await ReadFixtureSettingsAsync(service, assistant).ConfigureAwait(false),
                    Messages = new List<ChatCompletionMessage> { new ChatCompletionMessage { Role = "user", Content = "What is the rated speed?" } }
                }).ConfigureAwait(false);

                AssertHelper.IsTrue(result.Success, "chat success");
                AssertHelper.AreEqual(2, chat.Requests.Count, "answer regenerated once");
                AssertHelper.AreEqual("The engine is rated at 3000 rpm.", result.Response.Choices[0].Message.Content, "regenerated answer returned");
                AssertHelper.IsTrue(result.Response.Retrieval.AnswerRegenerated, "regeneration reported");
            });

            await ExecuteTestAsync("EvalService: judges with EvalJudgeInferenceEndpointId when it is set", async () =>
            {
                string source = System.IO.File.ReadAllText(System.IO.Path.Combine(GetRepositoryRoot(), "src", "AssistantHub.Core", "Services", "EvalService.cs"));
                AssertHelper.StringContains(source, "ResolveCompletionEndpointAsync(settings.EvalJudgeInferenceEndpointId)", "judge endpoint resolved");
                AssertHelper.StringContains(source, "judgeEndpoint = judgeResolved.Value.Endpoint", "judge endpoint used");
                await Task.CompletedTask;
            });

            #endregion

            #region Ingestion and crawling

            await ExecuteTestAsync("IngestionServiceBase.ChunkAndEmbedContentAsync: Structured mode sends one Partio cell per section, table and list", async () =>
            {
                RoutedChunkingService partio = new RoutedChunkingService();
                partio.Map("/v1.0/endpoints/embedding/", "{\"Id\":\"eep_1\",\"Model\":\"nomic-embed-text\",\"MaxConcurrentRequests\":4}");
                partio.Map("/v1.0/process/batch",
                    "[{\"Chunks\":[{\"Text\":\"section text\",\"Embeddings\":[0.1,0.2]}]},{\"Chunks\":[{\"Text\":\"Port: A\",\"Embeddings\":[0.3,0.4]}]}]");

                IngestionService ingestion = CreateTestIngestionService(new MockDatabaseDriver(), new VerbexSettings(), new RecordingInvertedIndexService());
                typeof(IngestionServiceBase).GetField("_ChunkingService", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ingestion, partio);

                IngestionRule rule = new IngestionRule
                {
                    Chunking = new IngestionChunkingConfig { CellMode = "Structured", ContextHeader = "Title" },
                    Embedding = new IngestionEmbeddingConfig { EmbeddingEndpointId = "eep_1", TaskPrefixes = true }
                };
                List<ExtractedBlock> blocks = new List<ExtractedBlock>
                {
                    new ExtractedBlock { Kind = "Heading", Text = "Setup", HeaderLevel = 1, PageNumber = 4 },
                    new ExtractedBlock { Kind = "Text", Text = "section text", PageNumber = 4 },
                    new ExtractedBlock { Kind = "Table", TableRows = new List<List<string>> { new List<string> { "Port" }, new List<string> { "A" } }, PageNumber = 5 }
                };

                MethodInfo method = typeof(IngestionServiceBase).GetMethod("ChunkAndEmbedContentAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Task task = (Task)method.Invoke(ingestion, new object[] { "adoc_s", "# Setup\n\nsection text", rule, new List<string> { "l1" }, new Dictionary<string, string> { ["k"] = "v" }, CancellationToken.None, blocks, "Manual" });
                await task.ConfigureAwait(false);
                System.Collections.IList chunks = (System.Collections.IList)task.GetType().GetProperty("Result").GetValue(task);

                AssertHelper.AreEqual(2, chunks.Count, "chunks from both cells");
                RecordedHttpCall batch = partio.Calls.First(c => c.Path == "/v1.0/process/batch");
                using JsonDocument body = JsonDocument.Parse(batch.Body);
                AssertHelper.AreEqual(2, body.RootElement.GetArrayLength(), "two cells");
                JsonElement textCell = body.RootElement[0];
                JsonElement tableCell = body.RootElement[1];
                AssertHelper.AreEqual("Text", textCell.GetProperty("Type").GetString(), "text cell");
                AssertHelper.AreEqual("search_document: Manual\n", textCell.GetProperty("ChunkingConfiguration").GetProperty("ContextPrefix").GetString(), "document prefix and title header");
                AssertHelper.AreEqual("00004", textCell.GetProperty("Tags").GetProperty(ProvenanceTags.PageStart).GetString(), "page tag on the cell");
                AssertHelper.AreEqual("v", textCell.GetProperty("Tags").GetProperty("k").GetString(), "rule tags kept");
                AssertHelper.AreEqual("Table", tableCell.GetProperty("Type").GetString(), "table cell");
                AssertHelper.AreEqual("RowGroupWithHeaders", tableCell.GetProperty("ChunkingConfiguration").GetProperty("Strategy").GetString(), "table strategy");
                AssertHelper.AreEqual("eep_1", tableCell.GetProperty("EmbeddingConfiguration").GetProperty("EmbeddingEndpointId").GetString(), "every cell carries its embedding config");
            });

            await ExecuteTestAsync("CrawlerBase.FindCrawledDocumentAsync: pages past the first 1000 documents", async () =>
            {
                MockDatabaseDriver database = new MockDatabaseDriver();
                DateTime baseTime = DateTime.UtcNow.AddDays(-1);
                for (int i = 0; i < 1105; i++)
                {
                    AssistantDocument doc = CreateToolDocument("adoc_page_" + i.ToString("D4"), "tenant_crawl", "col_c", "Doc " + i, DocumentStatusEnum.Completed);
                    doc.CreatedUtc = baseTime.AddSeconds(i);
                    doc.CrawlPlanId = "cplan_1";
                    doc.SourceUrl = "https://example.com/page/" + i;
                    await database.AssistantDocument.CreateAsync(doc).ConfigureAwait(false);
                }

                CrawlPlan plan = new CrawlPlan
                {
                    Id = "cplan_1",
                    TenantId = "tenant_crawl",
                    RepositoryType = RepositoryTypeEnum.Web,
                    RepositorySettings = new WebCrawlRepositorySettings { StartUrl = "https://example.com" }
                };

                using (CrawlerBase crawler = CrawlerFactory.Create(RepositoryTypeEnum.Web, CreateSilentLogging(), database, plan, new CrawlOperation(), null, null, null, "./crawl-enumerations/", CancellationToken.None))
                {
                    MethodInfo find = typeof(CrawlerBase).GetMethod("FindCrawledDocumentAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                    Task<AssistantDocument> oldest = (Task<AssistantDocument>)find.Invoke(crawler, new object[] { "https://example.com/page/0" });
                    AssertHelper.AreEqual("adoc_page_0000", (await oldest.ConfigureAwait(false))?.Id, "document on the second page found");

                    Task<AssistantDocument> missing = (Task<AssistantDocument>)find.Invoke(crawler, new object[] { "https://example.com/page/none" });
                    AssertHelper.IsNull(await missing.ConfigureAwait(false), "missing document");
                }
            });

            await ExecuteTestAsync("WebRepositoryCrawler: credentials are sent with the matching CrawlSharp authentication type", async () =>
            {
                (WebAuthTypeEnum Type, CrawlSharp.Web.AuthenticationTypeEnum Expected)[] cases =
                {
                    (WebAuthTypeEnum.None, CrawlSharp.Web.AuthenticationTypeEnum.None),
                    (WebAuthTypeEnum.Basic, CrawlSharp.Web.AuthenticationTypeEnum.Basic),
                    (WebAuthTypeEnum.ApiKey, CrawlSharp.Web.AuthenticationTypeEnum.ApiKey),
                    (WebAuthTypeEnum.BearerToken, CrawlSharp.Web.AuthenticationTypeEnum.BearerToken)
                };

                foreach ((WebAuthTypeEnum type, CrawlSharp.Web.AuthenticationTypeEnum expected) in cases)
                {
                    CrawlPlan plan = new CrawlPlan
                    {
                        Id = "cplan_auth",
                        TenantId = "tenant_crawl",
                        RepositoryType = RepositoryTypeEnum.Web,
                        RepositorySettings = new WebCrawlRepositorySettings
                        {
                            StartUrl = "https://example.com",
                            AuthenticationType = type,
                            Username = "crawler",
                            Password = "secret",
                            ApiKeyHeader = "x-api-key",
                            ApiKeyValue = "key",
                            BearerToken = "token"
                        }
                    };

                    using (CrawlerBase crawler = CrawlerFactory.Create(RepositoryTypeEnum.Web, CreateSilentLogging(), new MockDatabaseDriver(), plan, new CrawlOperation(), null, null, null, "./crawl-enumerations/", CancellationToken.None))
                    {
                        MethodInfo build = typeof(WebRepositoryCrawler).GetMethod("BuildSettings", BindingFlags.Instance | BindingFlags.NonPublic);
                        CrawlSharp.Web.Settings settings = (CrawlSharp.Web.Settings)build.Invoke(crawler, null);
                        AssertHelper.AreEqual(expected, settings.Authentication.Type, type + " authentication type");
                        if (type == WebAuthTypeEnum.Basic) AssertHelper.AreEqual("crawler", settings.Authentication.Username, "basic username");
                        if (type == WebAuthTypeEnum.BearerToken) AssertHelper.AreEqual("token", settings.Authentication.BearerToken, "bearer token");
                    }
                }

                await Task.CompletedTask;
            });

            await ExecuteTestAsync("WebRepositoryCrawler: authenticated crawl uses the crawl delay and survives a redirect loop", async () =>
            {
                using (CrawlStubServer site = new CrawlStubServer(GetAvailableTcpPort(), "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("crawler:secret"))))
                {
                    site.Start();
                    CrawlPlan plan = new CrawlPlan
                    {
                        Id = "cplan_live",
                        TenantId = "tenant_crawl",
                        RepositoryType = RepositoryTypeEnum.Web,
                        RepositorySettings = new WebCrawlRepositorySettings
                        {
                            StartUrl = site.BaseUrl,
                            AuthenticationType = WebAuthTypeEnum.Basic,
                            Username = "crawler",
                            Password = "secret",
                            IgnoreRobotsTxt = true,
                            CrawlDelayMs = 0,
                            MaxDepth = 3,
                            MaxParallelTasks = 1
                        }
                    };

                    List<string> keys = new List<string>();
                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                    using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                    using (CrawlerBase crawler = CrawlerFactory.Create(RepositoryTypeEnum.Web, CreateSilentLogging(), new MockDatabaseDriver(), plan, new CrawlOperation(), null, null, null, "./crawl-enumerations/", timeout.Token))
                    {
                        await foreach (CrawledObject crawled in crawler.EnumerateAsync(timeout.Token).ConfigureAwait(false))
                            keys.Add(crawled.Key);
                        AssertHelper.IsFalse(timeout.IsCancellationRequested, "crawl finished instead of hanging on the redirect loop");
                    }

                    sw.Stop();
                    AssertHelper.IsTrue(site.AuthorizedRequests > 0, "credentials were sent");
                    AssertHelper.AreEqual(0, site.UnauthorizedRequests, "every request, including redirect targets, carried credentials: " + String.Join(", ", site.UnauthorizedPaths));
                    AssertHelper.IsTrue(keys.Any(k => k.EndsWith("/page2", StringComparison.Ordinal)), "linked page crawled: " + String.Join(", ", keys));
                    AssertHelper.IsTrue(keys.Any(k => k.EndsWith("/moved", StringComparison.Ordinal)), "redirected page listed under its linking address: " + String.Join(", ", keys));
                    AssertHelper.IsFalse(keys.Any(k => k.Contains("/loop-", StringComparison.Ordinal)), "a redirect loop yields no page: " + String.Join(", ", keys));
                    AssertHelper.IsTrue(sw.Elapsed.TotalSeconds < 10, "crawl delay applied between requests (took " + sw.Elapsed.TotalSeconds.ToString("F1") + " s; CrawlSharp's 2.5 s default would take much longer)");
                }
            });

            await ExecuteTestAsync("WebRepositoryCrawler: redirect and credential-scope settings reach CrawlSharp and are validated", async () =>
            {
                WebCrawlRepositorySettings web = new WebCrawlRepositorySettings
                {
                    StartUrl = "https://example.com",
                    AuthenticationType = WebAuthTypeEnum.BearerToken,
                    BearerToken = "token",
                    FollowRedirects = true,
                    MaxRedirects = 3,
                    CredentialOrigins = new List<string> { "https://docs.example.com" }
                };
                CrawlPlan plan = new CrawlPlan { Id = "cplan_r", TenantId = "tenant_crawl", RepositoryType = RepositoryTypeEnum.Web, RepositorySettings = web };

                using (CrawlerBase crawler = CrawlerFactory.Create(RepositoryTypeEnum.Web, CreateSilentLogging(), new MockDatabaseDriver(), plan, new CrawlOperation(), null, null, null, "./crawl-enumerations/", CancellationToken.None))
                {
                    MethodInfo build = typeof(WebRepositoryCrawler).GetMethod("BuildSettings", BindingFlags.Instance | BindingFlags.NonPublic);
                    CrawlSharp.Web.Settings settings = (CrawlSharp.Web.Settings)build.Invoke(crawler, null);
                    AssertHelper.IsTrue(settings.Crawl.FollowRedirects, "redirects followed by CrawlSharp");
                    AssertHelper.AreEqual(3, settings.Crawl.MaxRedirects, "hop limit passed through");
                    AssertHelper.AreEqual("https://docs.example.com", settings.Authentication.CredentialOrigins.Single(), "credential origins passed through");
                }

                AssertHelper.AreEqual(50, new WebCrawlRepositorySettings { MaxRedirects = 500 }.MaxRedirects, "hop limit clamped to 50");
                AssertHelper.AreEqual(1, new WebCrawlRepositorySettings { MaxRedirects = 0 }.MaxRedirects, "hop limit clamped to 1");
                AssertHelper.HasCount(web.Validate(), 0, "valid settings");
                web.CredentialOrigins = new List<string> { "docs.example.com" };
                AssertHelper.HasCount(web.Validate(), 1, "a credential origin must be an absolute http(s) URL");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("NfsCrawlRepositorySettings.Validate: only NFSv3 is accepted (Blobject 6)", async () =>
            {
                NfsCrawlRepositorySettings nfs = new NfsCrawlRepositorySettings
                {
                    NfsHostname = "nfs-server",
                    NfsUserId = 1000,
                    NfsGroupId = 1000,
                    NfsShareName = "/exports/content",
                    NfsVersion = NfsVersionEnum.V3
                };
                AssertHelper.HasCount(nfs.Validate(), 0, "V3 accepted");
                nfs.NfsVersion = NfsVersionEnum.V4;
                AssertHelper.StringContains(String.Join(" ", nfs.Validate()), "V3 only", "V4 rejected");
                nfs.NfsVersion = NfsVersionEnum.V2;
                AssertHelper.HasCount(nfs.Validate(), 1, "V2 rejected");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("CIFS and NFS settings: path mistakes are rejected with guidance", async () =>
            {
                CifsCrawlRepositorySettings cifs = new CifsCrawlRepositorySettings { CifsHostname = "fileserver.example.com", CifsUsername = "svc", CifsPassword = "p", CifsShareName = "\\Documents\\" };
                AssertHelper.HasCount(cifs.Validate(), 0, "share with surrounding slashes accepted");
                cifs.CifsShareName = "Documents\\Policies";
                AssertHelper.StringContains(String.Join(" ", cifs.Validate()), "ObjectPrefix", "folder in share name rejected with a pointer to ObjectPrefix");
                cifs.CifsShareName = "Documents";
                cifs.CifsHostname = "\\\\fileserver\\Documents";
                AssertHelper.StringContains(String.Join(" ", cifs.Validate()), "not a UNC path", "UNC hostname rejected");
                cifs.CifsHostname = "smb://fileserver";
                AssertHelper.HasCount(cifs.Validate(), 1, "URL hostname rejected");

                NfsCrawlRepositorySettings nfs = new NfsCrawlRepositorySettings { NfsHostname = "nfs.example.com", NfsUserId = 1000, NfsGroupId = 1000, NfsShareName = "/exports/content" };
                AssertHelper.HasCount(nfs.Validate(), 0, "valid NFS settings");
                nfs.NfsShareName = "exports/content";
                AssertHelper.StringContains(String.Join(" ", nfs.Validate()), "starting with /", "relative export rejected");
                nfs.NfsShareName = "/exports/content";
                nfs.NfsHostname = "nfs.example.com:/exports/content";
                AssertHelper.StringContains(String.Join(" ", nfs.Validate()), "without the export path", "host:/export rejected");
                await Task.CompletedTask;
            });

            await ExecuteTestAsync("IngestionService: reprocessing deletes the previous chunk records after storing new ones", async () =>
            {
                string source = System.IO.File.ReadAllText(System.IO.Path.Combine(GetRepositoryRoot(), "src", "AssistantHub.Core", "Services", "IngestionService.cs"));
                int store = source.IndexOf("UpdateChunkRecordIdsAsync", StringComparison.Ordinal);
                int delete = source.IndexOf("DeleteEmbeddingBatchAsync(document.TenantId, collectionId, previousChunkIds", StringComparison.Ordinal);
                AssertHelper.IsTrue(store >= 0 && delete > store, "old records are deleted after the new ones are recorded");
                AssertHelper.StringContains(source, "UpdateContentHashAsync(documentId, contentSha256, nearDuplicates", "content hash and near-duplicates recorded");
                await Task.CompletedTask;
            });

            #endregion
        }

        #region Retrieval-Improvement-Helpers

        private static string ThreeChunkSearchResponse()
        {
            return "{\"Documents\":[" +
                "{\"DocumentId\":\"adoc_a\",\"Score\":0.9,\"Content\":\"alpha content\",\"Position\":0}," +
                "{\"DocumentId\":\"adoc_b\",\"Score\":0.8,\"Content\":\"the engine is rated at 3000 rpm\",\"Position\":0}," +
                "{\"DocumentId\":\"adoc_c\",\"Score\":0.7,\"Content\":\"gamma content\",\"Position\":0}]}";
        }

        private static HttpResponseMessage ChatReply(string content)
        {
            string body = JsonSerializer.Serialize(new
            {
                choices = new[] { new { finish_reason = "stop", message = new { role = "assistant", content = content } } },
                usage = new { prompt_tokens = 10, completion_tokens = 5, total_tokens = 15 }
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }

        private static async Task<AssistantSettings> ReadFixtureSettingsAsync(AssistantChatService service, Assistant assistant)
        {
            MockDatabaseDriver database = (MockDatabaseDriver)typeof(AssistantChatServiceBase)
                .GetField("_Database", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            return await database.AssistantSettings.ReadByAssistantIdAsync(assistant.Id).ConfigureAwait(false);
        }

        private static async Task<(AssistantChatService Service, Assistant Assistant, RecordingVectorStoreService VectorStore, MockHttpMessageHandler Chat)> CreateRetrievalFixtureAsync(
            Action<AssistantSettings> configure,
            MockHttpMessageHandler rerankHandler = null,
            Func<HttpRequestMessage, HttpResponseMessage> chatResponder = null,
            MockDatabaseDriver database = null,
            Dictionary<string, string> endpointTags = null,
            HttpMessageHandler chatHandlerOverride = null)
        {
            database ??= new MockDatabaseDriver();
            Assistant assistant = CreateToolAssistant();
            await database.Assistant.CreateAsync(assistant).ConfigureAwait(false);

            AssistantSettings settings = CreateToolSettings(new AssistantToolPolicy());
            settings.EnableRag = true;
            settings.EnableReranking = false;
            settings.EnableAnswerabilityCheck = false;
            settings.EnableRetrievalGate = false;
            settings.SearchMode = "FullText";
            settings.RetrievalIncludeNeighbors = 0;
            settings.RetrievalTopK = 5;
            settings.InferenceEndpointId = "cep_ri";
            configure?.Invoke(settings);
            await database.AssistantSettings.CreateAsync(settings).ConfigureAwait(false);

            MockHttpMessageHandler chat = new MockHttpMessageHandler();
            if (chatResponder != null) chat.When("chat/completions", chatResponder);
            HttpClient chatClient = chatHandlerOverride != null ? new HttpClient(chatHandlerOverride) : chat.CreateClient();

            AssistantHubSettings serverSettings = new AssistantHubSettings();
            serverSettings.Inference.RetryDelayMs = 1;
            serverSettings.Rerankers = new List<RerankerSettings>
            {
                new RerankerSettings { Id = "xenc", Name = "Cross-encoder", Endpoint = "http://reranker.test" }
            };

            InferenceService inference = new InferenceService(
                new InferenceSettings { Provider = InferenceProviderEnum.OpenAI, Endpoint = "https://openai-compatible.test/v1", ApiKey = "k", DefaultModel = "m" },
                CreateSilentLogging(),
                chatClient);

            RecordingVectorStoreService vectorStore = new RecordingVectorStoreService();
            RetrievalService retrieval = new RetrievalService(new ChunkingSettings(), new RecallDbSettings(), CreateSilentLogging(), vectorStore, new RecordingChunkingService());

            AssistantChatService service = new AssistantChatService(
                database,
                CreateSilentLogging(),
                serverSettings,
                retrieval,
                inference,
                inferenceEndpoints: new RecordingInferenceEndpointService(new PartioEndpointConfig
                {
                    Id = "cep_ri",
                    Endpoint = "https://openai-compatible.test/v1",
                    ApiFormat = "OpenAI",
                    ApiKey = "k",
                    Model = "m",
                    Active = true,
                    MaxConcurrentRequests = 4,
                    Tags = endpointTags
                }),
                rerankClient: new CrossEncoderRerankClient(rerankHandler?.CreateClient() ?? new MockHttpMessageHandler().CreateClient()));

            return (service, assistant, vectorStore, chat);
        }

        private static async Task<(AssistantChatService Service, Assistant Assistant, RecordingVectorStoreService VectorStore, MockDatabaseDriver Database)> CreateSupersessionFixtureAsync(string mode)
        {
            MockDatabaseDriver database = new MockDatabaseDriver();
            AssistantDocument old = CreateToolDocument("adoc_old", "tenant_tool", "col_tool", "Old", DocumentStatusEnum.Completed);
            AssistantDocument middle = CreateToolDocument("adoc_middle", "tenant_tool", "col_tool", "Middle", DocumentStatusEnum.Completed);
            AssistantDocument newest = CreateToolDocument("adoc_newest", "tenant_tool", "col_tool", "Newest", DocumentStatusEnum.Completed);
            AssistantDocument other = CreateToolDocument("adoc_other", "tenant_tool", "col_tool", "Other", DocumentStatusEnum.Completed);
            foreach (AssistantDocument d in new[] { old, middle, newest, other })
                await database.AssistantDocument.CreateAsync(d).ConfigureAwait(false);
            await DocumentSupersession.SetAsync(database, middle, new List<string> { "adoc_old" }).ConfigureAwait(false);
            await DocumentSupersession.SetAsync(database, newest, new List<string> { "adoc_middle" }).ConfigureAwait(false);

            (AssistantChatService service, Assistant assistant, RecordingVectorStoreService vectorStore, MockHttpMessageHandler chat) =
                await CreateRetrievalFixtureAsync(s => { s.SupersessionMode = mode; s.EnableDocumentAttachments = true; }, null, null, database).ConfigureAwait(false);
            return (service, assistant, vectorStore, database);
        }

        /// <summary>
        /// Chunking-service stub that answers by path prefix and records every call.
        /// </summary>
        private class RoutedChunkingService : IChunkingService
        {
            private readonly List<(string Path, string Body)> _Routes = new List<(string Path, string Body)>();

            public List<RecordedHttpCall> Calls { get; } = new List<RecordedHttpCall>();

            public void Map(string pathPrefix, string body)
            {
                _Routes.Add((pathPrefix, body));
            }

            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePathAndQuery, string body = null, CancellationToken token = default)
            {
                Calls.Add(new RecordedHttpCall { Method = method.Method, Path = relativePathAndQuery, Body = body });
                (string Path, string Body) route = _Routes.FirstOrDefault(r => relativePathAndQuery.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase));
                HttpResponseMessage response = new HttpResponseMessage(route.Path == null ? HttpStatusCode.NotFound : HttpStatusCode.OK)
                {
                    Content = new StringContent(route.Body ?? "{}", Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }

        /// <summary>
        /// A tiny website for crawl tests: Basic authentication, links, an ordinary redirect and a redirect loop.
        /// </summary>
        private sealed class CrawlStubServer : IDisposable
        {
            private readonly HttpListener _Listener = new HttpListener();
            private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
            private readonly string _ExpectedAuthorization;
            private Task _ListenerTask;
            private int _Authorized;
            private int _Unauthorized;

            public CrawlStubServer(int port, string expectedAuthorization)
            {
                BaseUrl = "http://127.0.0.1:" + port.ToString() + "/";
                _ExpectedAuthorization = expectedAuthorization;
                _Listener.Prefixes.Add(BaseUrl);
            }

            public string BaseUrl { get; }

            public int AuthorizedRequests => _Authorized;

            public int UnauthorizedRequests => _Unauthorized;

            public List<string> UnauthorizedPaths { get; } = new List<string>();

            public void Start()
            {
                _Listener.Start();
                _ListenerTask = Task.Run(() => ListenAsync(_TokenSource.Token));
            }

            public void Dispose()
            {
                _TokenSource.Cancel();
                try { _Listener.Stop(); _Listener.Close(); } catch { }
                try { _ListenerTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
                _TokenSource.Dispose();
            }

            private async Task ListenAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _Listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested || !_Listener.IsListening)
                    {
                        break;
                    }

                    _ = Task.Run(() => Handle(context));
                }
            }

            private void Handle(HttpListenerContext context)
            {
                try
                {
                    string path = context.Request.Url?.AbsolutePath ?? "/";
                    if (!String.Equals(context.Request.Headers["Authorization"], _ExpectedAuthorization, StringComparison.Ordinal))
                    {
                        Interlocked.Increment(ref _Unauthorized);
                        lock (UnauthorizedPaths) UnauthorizedPaths.Add(context.Request.HttpMethod + " " + path);
                        context.Response.StatusCode = 401;
                        context.Response.AddHeader("WWW-Authenticate", "Basic realm=\"stub\"");
                        return;
                    }

                    Interlocked.Increment(ref _Authorized);
                    switch (path)
                    {
                        case "/moved":
                            context.Response.StatusCode = 302;
                            context.Response.RedirectLocation = BaseUrl + "target";
                            return;
                        case "/loop-a":
                            context.Response.StatusCode = 302;
                            context.Response.RedirectLocation = BaseUrl + "loop-b";
                            return;
                        case "/loop-b":
                            context.Response.StatusCode = 302;
                            context.Response.RedirectLocation = BaseUrl + "loop-a";
                            return;
                    }

                    string body = path == "/"
                        ? "<html><body><h1>Home</h1><a href=\"/page2\">Two</a> <a href=\"/moved\">Moved</a> <a href=\"/loop-a\">Loop</a></body></html>"
                        : "<html><body><h1>Page " + path + "</h1><p>Content of " + path + ".</p></body></html>";
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/html";
                    context.Response.ContentLength64 = data.Length;
                    context.Response.OutputStream.Write(data, 0, data.Length);
                }
                catch
                {
                }
                finally
                {
                    try { context.Response.Close(); } catch { }
                }
            }
        }

        /// <summary>
        /// HTTP stub that answers mapped paths and records request bodies.
        /// </summary>
        private sealed class PathStubServer : IDisposable
        {
            private readonly HttpListener _Listener = new HttpListener();
            private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
            private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _Routes = new Dictionary<string, (HttpStatusCode Status, string Body)>(StringComparer.OrdinalIgnoreCase);
            private Task _ListenerTask;

            public PathStubServer(int port)
            {
                BaseUrl = "http://127.0.0.1:" + port.ToString() + "/";
                _Listener.Prefixes.Add(BaseUrl);
            }

            public string BaseUrl { get; }

            public List<string> Bodies { get; } = new List<string>();

            public void Map(string path, HttpStatusCode status, string body)
            {
                _Routes[path] = (status, body);
            }

            public void Start()
            {
                _Listener.Start();
                _ListenerTask = Task.Run(() => ListenAsync(_TokenSource.Token));
            }

            public void Dispose()
            {
                _TokenSource.Cancel();
                try { _Listener.Stop(); _Listener.Close(); } catch { }
                try { _ListenerTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
                _TokenSource.Dispose();
            }

            private async Task ListenAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _Listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested || !_Listener.IsListening)
                    {
                        break;
                    }

                    try
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(context.Request.InputStream, Encoding.UTF8))
                        {
                            string requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                            lock (Bodies) Bodies.Add(requestBody);
                        }

                        string path = context.Request.Url?.AbsolutePath ?? "/";
                        if (_Routes.TryGetValue(path, out (HttpStatusCode Status, string Body) route))
                        {
                            byte[] data = Encoding.UTF8.GetBytes(route.Body ?? "");
                            context.Response.StatusCode = (int)route.Status;
                            context.Response.ContentType = "application/json";
                            context.Response.ContentLength64 = data.Length;
                            await context.Response.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        try { context.Response.OutputStream.Close(); } catch { }
                    }
                }
            }
        }

        #endregion
    }
}
