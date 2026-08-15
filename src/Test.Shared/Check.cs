namespace Test.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Lightweight, framework-agnostic assertion helpers. Touchstone treats a test case
    /// as failed when its delegate throws, so every helper below throws
    /// <see cref="TestAssertionException"/> on failure and returns silently on success.
    /// This keeps the shared suites free of any xUnit/NUnit/MSTest dependency.
    /// </summary>
    public static class Check
    {
        /// <summary>
        /// Assert that a condition is true.
        /// </summary>
        /// <param name="condition">Condition that must be true.</param>
        /// <param name="message">Failure message.</param>
        public static void True(bool condition, string message = null)
        {
            if (!condition) throw new TestAssertionException(message ?? "Expected condition to be true.");
        }

        /// <summary>
        /// Assert that a condition is false.
        /// </summary>
        /// <param name="condition">Condition that must be false.</param>
        /// <param name="message">Failure message.</param>
        public static void False(bool condition, string message = null)
        {
            if (condition) throw new TestAssertionException(message ?? "Expected condition to be false.");
        }

        /// <summary>
        /// Assert that an object is null.
        /// </summary>
        /// <param name="value">Value that must be null.</param>
        /// <param name="message">Failure message.</param>
        public static void Null(object value, string message = null)
        {
            if (value != null) throw new TestAssertionException(message ?? "Expected value to be null, but it was not.");
        }

        /// <summary>
        /// Assert that an object is not null.
        /// </summary>
        /// <param name="value">Value that must not be null.</param>
        /// <param name="message">Failure message.</param>
        public static void NotNull(object value, string message = null)
        {
            if (value == null) throw new TestAssertionException(message ?? "Expected value to be non-null, but it was null.");
        }

        /// <summary>
        /// Assert value equality using the default comparer.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="expected">Expected value.</param>
        /// <param name="actual">Actual value.</param>
        /// <param name="message">Failure message.</param>
        public static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new TestAssertionException(
                    (message ?? "Values are not equal.") +
                    " Expected: [" + Format(expected) + "], Actual: [" + Format(actual) + "].");
            }
        }

        /// <summary>
        /// Assert that a string contains a substring (ordinal).
        /// </summary>
        /// <param name="expectedSubstring">Substring expected to be present.</param>
        /// <param name="actual">Actual string.</param>
        /// <param name="message">Failure message.</param>
        public static void Contains(string expectedSubstring, string actual, string message = null)
        {
            if (actual == null || actual.IndexOf(expectedSubstring ?? String.Empty, StringComparison.Ordinal) < 0)
            {
                throw new TestAssertionException(
                    (message ?? "Substring not found.") +
                    " Expected to contain: [" + expectedSubstring + "], Actual: [" + Format(actual) + "].");
            }
        }

        /// <summary>
        /// Assert that a collection has the expected number of elements.
        /// </summary>
        /// <param name="expectedCount">Expected count.</param>
        /// <param name="collection">Collection under test.</param>
        /// <param name="message">Failure message.</param>
        public static void Count(int expectedCount, ICollection collection, string message = null)
        {
            NotNull(collection, message ?? "Expected a non-null collection.");
            if (collection.Count != expectedCount)
            {
                throw new TestAssertionException(
                    (message ?? "Collection count mismatch.") +
                    " Expected count: " + expectedCount + ", Actual count: " + collection.Count + ".");
            }
        }

        /// <summary>
        /// Assert that a synchronous action throws the exact exception type
        /// <typeparamref name="TException"/> (assignable, so derived types satisfy the check).
        /// </summary>
        /// <typeparam name="TException">Expected exception type.</typeparam>
        /// <param name="action">Action expected to throw.</param>
        public static void Throws<TException>(Action action) where TException : Exception
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (ex is TException) return;
                throw new TestAssertionException(
                    "Expected exception of type " + typeof(TException).Name +
                    " but caught " + ex.GetType().Name + ": " + ex.Message);
            }

            throw new TestAssertionException(
                "Expected exception of type " + typeof(TException).Name + " but no exception was thrown.");
        }

        /// <summary>
        /// Assert that an asynchronous action throws the exception type
        /// <typeparamref name="TException"/> (assignable, so derived types satisfy the check).
        /// </summary>
        /// <typeparam name="TException">Expected exception type.</typeparam>
        /// <param name="action">Async action expected to throw.</param>
        /// <returns>Task.</returns>
        public static async Task ThrowsAsync<TException>(Func<Task> action) where TException : Exception
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is TException) return;
                throw new TestAssertionException(
                    "Expected exception of type " + typeof(TException).Name +
                    " but caught " + ex.GetType().Name + ": " + ex.Message);
            }

            throw new TestAssertionException(
                "Expected exception of type " + typeof(TException).Name + " but no exception was thrown.");
        }

        private static string Format(object value)
        {
            if (value == null) return "null";
            return value.ToString();
        }
    }
}
