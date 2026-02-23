namespace Test.Database
{
    using System;
    using System.Collections.Generic;

    public static class AssertHelper
    {
        public static void IsNotNull(object value, string name)
        {
            if (value == null)
                throw new Exception($"Expected {name} to be non-null, but was null.");
        }

        public static void IsNull(object value, string name)
        {
            if (value != null)
                throw new Exception($"Expected {name} to be null, but was '{value}'.");
        }

        public static void AreEqual<T>(T expected, T actual, string name)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"Expected {name} to be '{expected}', but was '{actual}'.");
        }

        public static void AreNotEqual<T>(T expected, T actual, string name)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"Expected {name} to differ from '{expected}', but they were equal.");
        }

        public static void IsTrue(bool value, string message)
        {
            if (!value)
                throw new Exception($"Assertion failed: {message}");
        }

        public static void IsFalse(bool value, string message)
        {
            if (value)
                throw new Exception($"Assertion failed (expected false): {message}");
        }

        public static void IsGreaterThan(long actual, long minimum, string name)
        {
            if (actual <= minimum)
                throw new Exception($"Expected {name} to be greater than {minimum}, but was {actual}.");
        }

        public static void IsGreaterThanOrEqual(long actual, long minimum, string name)
        {
            if (actual < minimum)
                throw new Exception($"Expected {name} to be >= {minimum}, but was {actual}.");
        }

        public static void StartsWith(string value, string prefix, string name)
        {
            if (value == null || !value.StartsWith(prefix))
                throw new Exception($"Expected {name} to start with '{prefix}', but was '{value}'.");
        }

        public static void DateTimeRecent(DateTime value, string name, int toleranceSeconds = 30)
        {
            double diff = Math.Abs((DateTime.UtcNow - value).TotalSeconds);
            if (diff > toleranceSeconds)
                throw new Exception($"Expected {name} to be recent (within {toleranceSeconds}s), but was {value:o} ({diff:F0}s ago).");
        }

        public static void DateTimeNullableRecent(DateTime? value, string name, int toleranceSeconds = 30)
        {
            if (value == null) return;
            DateTimeRecent(value.Value, name, toleranceSeconds);
        }

        public static void ThrowsAsync<TException>(Func<Task> action, string description) where TException : Exception
        {
            try
            {
                action().GetAwaiter().GetResult();
                throw new Exception($"Expected {typeof(TException).Name} for: {description}");
            }
            catch (TException)
            {
                // expected
            }
        }
    }
}
