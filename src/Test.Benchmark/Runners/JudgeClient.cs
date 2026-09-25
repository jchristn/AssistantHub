namespace Test.Benchmark.Runners
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// LLM-as-judge, called directly against a model endpoint and never through AssistantHub, so grading is
    /// independent of the system under test. Uses yes/no grading against a gold answer (LongMemEval style) for
    /// correctness and abstention, and a context-support check for faithfulness.
    /// </summary>
    public class JudgeClient
    {
        #region Public-Members

        /// <summary>
        /// Description of the judge (format, model, endpoint).
        /// </summary>
        public string Description { get; }

        #endregion

        #region Private-Members

        private readonly HttpClient _Http;
        private readonly string _Format;
        private readonly string _BaseUrl;
        private readonly string _Model;
        private readonly string? _ApiKey;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="http">HTTP client.</param>
        /// <param name="format">Ollama or OpenAI (any OpenAI-compatible chat completions endpoint).</param>
        /// <param name="baseUrl">Base URL.</param>
        /// <param name="model">Model.</param>
        /// <param name="apiKey">Optional bearer key.</param>
        public JudgeClient(HttpClient http, string format, string baseUrl, string model, string? apiKey)
        {
            _Http = http ?? throw new ArgumentNullException(nameof(http));
            _Format = format;
            _BaseUrl = baseUrl.TrimEnd('/');
            _Model = model;
            _ApiKey = apiKey;
            Description = format + " " + model + " @ " + _BaseUrl;
        }

        /// <summary>
        /// Build a judge from arguments, or null when <c>--judge-format none</c>.
        /// </summary>
        /// <param name="http">HTTP client.</param>
        /// <param name="args">Arguments.</param>
        /// <param name="ollamaUrl">Default Ollama URL.</param>
        /// <returns>Judge or null.</returns>
        public static JudgeClient? From(HttpClient http, BenchmarkArguments args, string ollamaUrl)
        {
            string format = args.Get("judge-format", "Ollama");
            if (string.Equals(format, "none", StringComparison.OrdinalIgnoreCase)) return null;
            return new JudgeClient(http, format, args.Get("judge-url", ollamaUrl), args.Get("judge-model", "gemma3:4b"), args.GetOptional("judge-api-key"));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Grade an answer against a gold answer.
        /// </summary>
        /// <param name="question">Question.</param>
        /// <param name="gold">Gold answer, or NOT_IN_CORPUS.</param>
        /// <param name="response">Answer.</param>
        /// <param name="unanswerable">True when the corpus does not answer the question.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Verdict, or null when the judge reply could not be parsed.</returns>
        public async Task<bool?> GradeAsync(string question, string gold, string response, bool unanswerable, CancellationToken token)
        {
            string prompt;
            if (unanswerable)
            {
                prompt = "I will give you a question and a response from an assistant that answers only from a document collection. The documents do NOT contain the answer. "
                    + "Answer yes if the response says it does not know, cannot find it, or that the documents do not contain the answer (it may mention related information it did find). "
                    + "Answer no if the response asserts a specific answer to the question.\n\n"
                    + "Question: " + question + "\n\nResponse: " + response + "\n\nDoes the response correctly decline to answer? Answer yes or no only.";
            }
            else
            {
                prompt = "I will give you a question, a correct answer, and a response from a model. Answer yes if the response contains the correct answer "
                    + "or is equivalent to it (paraphrases, different formatting of numbers, and extra detail are fine). Answer no if the response is wrong, contradicts the correct answer, "
                    + "only gives part of a multi-part answer, or says it does not know.\n\n"
                    + "Question: " + question + "\n\nCorrect answer: " + gold + "\n\nModel response: " + response
                    + "\n\nIs the model response correct? Answer yes or no only.";
            }

            return ParseYesNo(await CompleteAsync(prompt, token).ConfigureAwait(false));
        }

        /// <summary>
        /// Judge whether every factual claim in an answer is supported by the context it was given.
        /// </summary>
        /// <param name="question">Question.</param>
        /// <param name="context">Injected context.</param>
        /// <param name="response">Answer.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Verdict, or null when unparseable.</returns>
        public async Task<bool?> FaithfulAsync(string question, string context, string response, CancellationToken token)
        {
            if (context.Length > 24000) context = context.Substring(0, 24000);
            string prompt = "I will give you retrieved context, a question, and a response. Answer yes if every factual claim in the response is stated in or directly "
                + "follows from the context (statements that the context does not contain something are fine). Answer no if the response states any fact that the context "
                + "does not support, even if that fact is true in general.\n\nContext:\n" + context + "\n\nQuestion: " + question + "\n\nResponse: " + response
                + "\n\nIs every claim in the response supported by the context? Answer yes or no only.";
            return ParseYesNo(await CompleteAsync(prompt, token).ConfigureAwait(false));
        }

        /// <summary>
        /// Remove thinking blocks some reasoning models emit inline.
        /// </summary>
        /// <param name="text">Model output.</param>
        /// <returns>Text without thinking blocks.</returns>
        public static string StripThinking(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string stripped = Regex.Replace(text, "<think>.*?</think>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            int open = stripped.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
            return open >= 0 ? stripped.Substring(0, open) : stripped;
        }

        #endregion

        #region Private-Methods

        private static bool? ParseYesNo(string reply)
        {
            string cleaned = StripThinking(reply).Trim().ToLowerInvariant().TrimStart('*', '"', '\'', ' ');
            if (cleaned.StartsWith("yes", StringComparison.Ordinal)) return true;
            if (cleaned.StartsWith("no", StringComparison.Ordinal)) return false;
            bool yes = Regex.IsMatch(cleaned, "\\byes\\b");
            bool no = Regex.IsMatch(cleaned, "\\bno\\b");
            if (yes && !no) return true;
            if (no && !yes) return false;
            return null;
        }

        private async Task<string> CompleteAsync(string prompt, CancellationToken token)
        {
            bool ollama = string.Equals(_Format, "Ollama", StringComparison.OrdinalIgnoreCase);
            JsonObject body;
            string path;
            if (ollama)
            {
                path = "/api/chat";
                body = new JsonObject
                {
                    ["model"] = _Model,
                    ["stream"] = false,
                    ["think"] = false,
                    ["options"] = new JsonObject { ["temperature"] = 0, ["num_ctx"] = 8192 },
                    ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = prompt } }
                };
            }
            else
            {
                path = "/v1/chat/completions";
                body = new JsonObject
                {
                    ["model"] = _Model,
                    ["temperature"] = 0,
                    ["max_tokens"] = 16,
                    ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = prompt } }
                };
            }

            for (int attempt = 0; ; attempt++)
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _BaseUrl + path);
                request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(_ApiKey)) request.Headers.Add("Authorization", "Bearer " + _ApiKey);
                HttpResponseMessage response;
                try
                {
                    response = await _Http.SendAsync(request, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException e) when (!token.IsCancellationRequested)
                {
                    throw new InvalidOperationException("Judge request timed out: " + e.Message);
                }
                catch (HttpRequestException e)
                {
                    throw new InvalidOperationException("Judge request failed: " + e.Message);
                }

                using HttpResponseMessage disposable = response;
                string text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < 4 && ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500))
                    {
                        await Task.Delay(3000 * (attempt + 1), token).ConfigureAwait(false);
                        continue;
                    }

                    throw new InvalidOperationException("Judge returned " + (int)response.StatusCode + ": " + text);
                }

                JsonNode? parsed = JsonNode.Parse(text);
                if (ollama) return parsed?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
                return parsed?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
            }
        }

        #endregion
    }
}
