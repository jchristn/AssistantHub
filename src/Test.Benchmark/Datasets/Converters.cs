namespace Test.Benchmark.Datasets
{
    using System;
    using System.Linq;

    /// <summary>
    /// The <c>prepare</c> command: converts public datasets into the harness format.
    /// </summary>
    public static class Converters
    {
        #region Public-Methods

        /// <summary>
        /// Run the prepare command.
        /// </summary>
        /// <param name="args">Arguments (--format, --input, --output, --name, --split, --limit, --seed).</param>
        /// <returns>Exit code.</returns>
        public static int Prepare(BenchmarkArguments args)
        {
            string format = args.Get("format", string.Empty).ToLowerInvariant();
            string input = args.Get("input", string.Empty);
            string output = args.Get("output", string.Empty);
            if (string.IsNullOrEmpty(format) || string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
                throw new ArgumentException("prepare needs --format beir|multihop-rag|qasper --input <path> --output <file.json>.");

            int seed = args.GetInt("seed", 7);
            BenchmarkDataset dataset = format switch
            {
                "beir" => BeirConverter.Convert(input, args.Get("name", System.IO.Path.GetFileName(input.TrimEnd('/', '\\'))), args.Get("split", "test")),
                "multihop-rag" => MultiHopRagConverter.Convert(input, args.GetInt("limit", 300), seed),
                "qasper" => QasperConverter.Convert(input, args.GetInt("papers", 50), seed),
                _ => throw new ArgumentException("Unknown format '" + format + "'.")
            };

            if (format == "beir" && args.GetInt("limit", 0) > 0)
            {
                BenchmarkCorpus corpus = dataset.Corpora[0];
                corpus.Queries = MultiHopRagConverter.Sample(corpus.Queries, args.GetInt("limit", 0), seed);
            }

            DatasetStore.Save(dataset, output);
            Console.WriteLine("Wrote " + output + ": " + dataset.Corpora.Sum(c => c.Documents.Count) + " documents, " + dataset.Corpora.Sum(c => c.Queries.Count) + " queries");
            return 0;
        }

        #endregion
    }
}
