namespace Test.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Minimal command-line parser: the first bare word is the command, and <c>--name value</c> or <c>--flag</c>
    /// pairs are options.
    /// </summary>
    public class BenchmarkArguments
    {
        #region Public-Members

        /// <summary>
        /// The command (first positional argument), lowercased.
        /// </summary>
        public string Command { get; private set; } = string.Empty;

        #endregion

        #region Private-Members

        private readonly Dictionary<string, string> _Options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Parse command-line arguments.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Parsed arguments.</returns>
        public static BenchmarkArguments Parse(string[] args)
        {
            BenchmarkArguments parsed = new BenchmarkArguments();
            if (args == null) return parsed;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    string name = arg.Substring(2);
                    bool hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
                    parsed._Options[name] = hasValue ? args[++i] : "true";
                }
                else if (string.IsNullOrEmpty(parsed.Command))
                {
                    parsed.Command = arg.ToLowerInvariant();
                }
            }

            return parsed;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get an option or a default.
        /// </summary>
        /// <param name="name">Option name without dashes.</param>
        /// <param name="defaultValue">Default.</param>
        /// <returns>Value.</returns>
        public string Get(string name, string defaultValue)
        {
            return _Options.TryGetValue(name, out string? value) ? value : defaultValue;
        }

        /// <summary>
        /// Get an option or null.
        /// </summary>
        /// <param name="name">Option name without dashes.</param>
        /// <returns>Value or null.</returns>
        public string? GetOptional(string name)
        {
            return _Options.TryGetValue(name, out string? value) ? value : null;
        }

        /// <summary>
        /// Get an integer option.
        /// </summary>
        /// <param name="name">Option name.</param>
        /// <param name="defaultValue">Default.</param>
        /// <returns>Value.</returns>
        public int GetInt(string name, int defaultValue)
        {
            return _Options.TryGetValue(name, out string? value) ? int.Parse(value, CultureInfo.InvariantCulture) : defaultValue;
        }

        /// <summary>
        /// Get a floating-point option.
        /// </summary>
        /// <param name="name">Option name.</param>
        /// <param name="defaultValue">Default.</param>
        /// <returns>Value.</returns>
        public double GetDouble(string name, double defaultValue)
        {
            return _Options.TryGetValue(name, out string? value) ? double.Parse(value, CultureInfo.InvariantCulture) : defaultValue;
        }

        /// <summary>
        /// True when a flag is present and not "false".
        /// </summary>
        /// <param name="name">Option name.</param>
        /// <returns>Flag value.</returns>
        public bool GetFlag(string name)
        {
            return _Options.TryGetValue(name, out string? value) && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A comma-separated option as a list.
        /// </summary>
        /// <param name="name">Option name.</param>
        /// <param name="defaultValue">Default (comma-separated).</param>
        /// <returns>Items.</returns>
        public List<string> GetList(string name, string defaultValue)
        {
            List<string> items = new List<string>();
            foreach (string part in Get(name, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                items.Add(part);
            }

            return items;
        }

        /// <summary>
        /// All options (for recording in reports).
        /// </summary>
        /// <returns>Copy of the options.</returns>
        public Dictionary<string, string> All()
        {
            return new Dictionary<string, string>(_Options, StringComparer.OrdinalIgnoreCase);
        }

        #endregion
    }
}
