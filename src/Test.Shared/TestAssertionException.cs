namespace Test.Shared
{
    using System;

    /// <summary>
    /// Exception thrown by <see cref="Check"/> when an assertion fails. Touchstone captures
    /// the message and stack trace and reports the owning test case as failed.
    /// </summary>
    public sealed class TestAssertionException : Exception
    {
        /// <summary>
        /// Initialize with a failure message.
        /// </summary>
        /// <param name="message">Failure message.</param>
        public TestAssertionException(string message) : base(message)
        {
        }
    }
}
