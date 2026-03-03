namespace Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        // --- Collection assertions ---

        public static void HasCount<T>(ICollection<T> collection, int expected, string name)
        {
            IsNotNull(collection, name);
            if (collection.Count != expected)
                throw new Exception($"Expected {name} to have {expected} items, but had {collection.Count}.");
        }

        public static void Contains<T>(IEnumerable<T> collection, T item, string name)
        {
            IsNotNull(collection, name);
            if (!collection.Contains(item))
                throw new Exception($"Expected {name} to contain '{item}', but it did not.");
        }

        public static void IsEmpty<T>(ICollection<T> collection, string name)
        {
            IsNotNull(collection, name);
            if (collection.Count != 0)
                throw new Exception($"Expected {name} to be empty, but had {collection.Count} items.");
        }

        public static void AllMatch<T>(IEnumerable<T> collection, Func<T, bool> predicate, string name)
        {
            IsNotNull(collection, name);
            int index = 0;
            foreach (T item in collection)
            {
                if (!predicate(item))
                    throw new Exception($"Expected all items in {name} to match predicate, but item at index {index} ('{item}') did not.");
                index++;
            }
        }

        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string name)
        {
            IsNotNull(collection, name);
            if (collection.Contains(item))
                throw new Exception($"Expected {name} to not contain '{item}', but it did.");
        }

        public static void InRange(double actual, double min, double max, string name)
        {
            if (actual < min || actual > max)
                throw new Exception($"Expected {name} to be in range [{min}, {max}], but was {actual}.");
        }

        public static void StringContains(string actual, string substring, string name)
        {
            IsNotNull(actual, name);
            if (!actual.Contains(substring))
                throw new Exception($"Expected {name} to contain '{substring}', but was '{actual}'.");
        }
    }
}
