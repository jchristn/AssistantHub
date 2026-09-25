namespace Test.Benchmark.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Statistics for reporting uncertainty: bootstrap confidence intervals, a paired bootstrap significance test,
    /// AUROC and Cohen's kappa. Resampling uses a fixed seed so reports are reproducible.
    /// </summary>
    public static class Statistics
    {
        #region Public-Members

        /// <summary>
        /// Bootstrap resamples.
        /// </summary>
        public const int Resamples = 1000;

        /// <summary>
        /// Resampling seed.
        /// </summary>
        public const int Seed = 20260924;

        #endregion

        #region Public-Methods

        /// <summary>
        /// 95% percentile-bootstrap confidence interval of the mean.
        /// </summary>
        /// <param name="values">Per-query values.</param>
        /// <returns>Interval, or null when fewer than two values.</returns>
        public static ConfidenceInterval? BootstrapMean(IReadOnlyList<double> values)
        {
            if (values == null || values.Count < 2) return null;
            Random random = new Random(Seed);
            double[] means = new double[Resamples];
            for (int r = 0; r < Resamples; r++)
            {
                double sum = 0.0;
                for (int i = 0; i < values.Count; i++) sum += values[random.Next(values.Count)];
                means[r] = sum / values.Count;
            }

            Array.Sort(means);
            return new ConfidenceInterval
            {
                Mean = Math.Round(values.Average(), 4),
                Low = Math.Round(means[(int)(0.025 * Resamples)], 4),
                High = Math.Round(means[(int)(0.975 * Resamples) - 1], 4),
                N = values.Count
            };
        }

        /// <summary>
        /// Paired bootstrap test on per-query differences (candidate minus baseline). Returns the two-sided p-value
        /// for "the mean difference is zero": the share of resampled mean differences on the far side of zero,
        /// doubled.
        /// </summary>
        /// <param name="differences">Per-query differences.</param>
        /// <returns>p-value in [0, 1], or null when fewer than two pairs.</returns>
        public static double? PairedBootstrapP(IReadOnlyList<double> differences)
        {
            if (differences == null || differences.Count < 2) return null;
            double observed = differences.Average();
            if (differences.All(d => d == 0.0)) return 1.0;
            Random random = new Random(Seed);
            int opposite = 0;
            for (int r = 0; r < Resamples; r++)
            {
                double sum = 0.0;
                for (int i = 0; i < differences.Count; i++) sum += differences[random.Next(differences.Count)];
                double mean = sum / differences.Count;
                if ((observed > 0 && mean <= 0) || (observed < 0 && mean >= 0)) opposite++;
            }

            return Math.Min(1.0, Math.Round(2.0 * (opposite + 1) / (Resamples + 1), 4));
        }

        /// <summary>
        /// AUROC (Mann-Whitney form): probability a random positive scores above a random negative, ties half.
        /// </summary>
        /// <param name="positives">Scores of positives.</param>
        /// <param name="negatives">Scores of negatives.</param>
        /// <returns>AUROC, or null without both classes.</returns>
        public static double? Auroc(IReadOnlyList<double> positives, IReadOnlyList<double> negatives)
        {
            if (positives == null || negatives == null || positives.Count == 0 || negatives.Count == 0) return null;
            double wins = 0.0;
            foreach (double p in positives)
            {
                foreach (double n in negatives)
                {
                    if (p > n) wins += 1.0;
                    else if (p == n) wins += 0.5;
                }
            }

            return Math.Round(wins / (positives.Count * (double)negatives.Count), 4);
        }

        /// <summary>
        /// Cohen's kappa between two binary raters.
        /// </summary>
        /// <param name="pairs">Paired verdicts.</param>
        /// <returns>Kappa, or null when there are no pairs.</returns>
        public static double? CohensKappa(IReadOnlyList<(bool A, bool B)> pairs)
        {
            if (pairs == null || pairs.Count == 0) return null;
            double n = pairs.Count;
            double agree = pairs.Count(p => p.A == p.B) / n;
            double aYes = pairs.Count(p => p.A) / n;
            double bYes = pairs.Count(p => p.B) / n;
            double chance = aYes * bYes + (1 - aYes) * (1 - bYes);
            if (Math.Abs(1 - chance) < 1e-12) return agree >= 1.0 ? 1.0 : 0.0;
            return Math.Round((agree - chance) / (1 - chance), 4);
        }

        /// <summary>
        /// Sample standard deviation.
        /// </summary>
        /// <param name="values">Values.</param>
        /// <returns>Standard deviation, 0 for fewer than two values.</returns>
        public static double StandardDeviation(IReadOnlyList<double> values)
        {
            if (values == null || values.Count < 2) return 0.0;
            double mean = values.Average();
            return Math.Round(Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1)), 4);
        }

        #endregion
    }
}
