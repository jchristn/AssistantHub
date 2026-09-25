namespace Test.Benchmark.Runners
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Test.Benchmark.Datasets;
    using Test.Benchmark.Metrics;

    /// <summary>
    /// Judge calibration against hand labels. <c>label-sample</c> writes a stratified sample of judged answers to a
    /// file with a <c>human</c> field to fill in; <c>chat --hand-labels</c> reads the filled file and reports the
    /// judge's agreement (raw and Cohen's kappa) with the human verdicts.
    /// </summary>
    public static class JudgeAgreement
    {
        #region Public-Methods

        /// <summary>
        /// Write a labelling sample from a chat report.
        /// </summary>
        /// <param name="args">Arguments (--report, --count, --output, --seed).</param>
        /// <returns>Exit code.</returns>
        public static int WriteLabelSample(BenchmarkArguments args)
        {
            string reportPath = args.Get("report", string.Empty);
            if (!File.Exists(reportPath)) throw new FileNotFoundException("label-sample needs --report <chat report .json>.");
            ChatReport report = JsonSerializer.Deserialize<ChatReport>(File.ReadAllText(reportPath), DatasetStore.Json) ?? throw new InvalidDataException("Not a chat report.");
            int count = args.GetInt("count", 50);
            Random random = new Random(args.GetInt("seed", 7));

            List<ChatItem> judged = report.Items.Where(i => i.Repeat == 0 && i.Correct.HasValue && i.Error == null).ToList();
            List<ChatItem> sample = judged
                .GroupBy(i => i.Type)
                .SelectMany(g => g.OrderBy(_ => random.Next()))
                .OrderBy(_ => random.Next())
                .Take(count)
                .ToList();

            JsonArray items = new JsonArray();
            foreach (ChatItem item in sample)
            {
                items.Add(new JsonObject
                {
                    ["key"] = item.Corpus + "/" + item.QueryId,
                    ["type"] = item.Type,
                    ["question"] = item.Question,
                    ["gold"] = item.Gold,
                    ["answer"] = item.Answer,
                    ["judge"] = item.Correct,
                    ["human"] = null
                });
            }

            string output = args.Get("output", Path.ChangeExtension(reportPath, ".labels.json"));
            File.WriteAllText(output, items.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            Console.WriteLine("Wrote " + sample.Count + " items to " + output + ". Set \"human\" to true or false for each, then run chat with --hand-labels " + output);
            return 0;
        }

        /// <summary>
        /// Agreement of the judge with hand labels.
        /// </summary>
        /// <param name="labelsPath">Filled labels file.</param>
        /// <param name="items">Judged items of this run.</param>
        /// <returns>Agreement statistics.</returns>
        public static Dictionary<string, double> Compute(string labelsPath, List<ChatItem> items)
        {
            JsonArray labels = JsonNode.Parse(File.ReadAllText(labelsPath)) as JsonArray ?? throw new InvalidDataException("Labels file must be a JSON array.");
            Dictionary<string, bool> human = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (JsonNode? label in labels)
            {
                string? key = label?["key"]?.GetValue<string>();
                JsonNode? verdict = label?["human"];
                if (key != null && verdict != null) human[key] = verdict.GetValue<bool>();
            }

            List<(bool A, bool B)> pairs = items
                .Where(i => i.Correct.HasValue && human.ContainsKey(i.Corpus + "/" + i.QueryId))
                .Select(i => (i.Correct!.Value, human[i.Corpus + "/" + i.QueryId]))
                .ToList();

            Dictionary<string, double> result = new Dictionary<string, double> { ["labelled"] = human.Count, ["paired"] = pairs.Count };
            if (pairs.Count > 0)
            {
                result["agreement"] = Math.Round(pairs.Count(p => p.A == p.B) / (double)pairs.Count, 4);
                result["kappa"] = Statistics.CohensKappa(pairs) ?? 0;
                result["judgeLenient"] = pairs.Count(p => p.A && !p.B);
                result["judgeStrict"] = pairs.Count(p => !p.A && p.B);
            }

            return result;
        }

        #endregion
    }
}
