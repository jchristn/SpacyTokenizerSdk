namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Test.Shared;

    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit host for the shared SpacyTokenizerSdk suites via the Touchstone NUnit adapter.
    /// <see cref="TouchstoneTestCaseSource"/> yields each non-skipped descriptor as a
    /// data-driven case so failures are reported per test case. The test logic itself lives
    /// once in <see cref="SpacyTokenizerSuites"/>.
    /// </summary>
    [TestFixture]
    public sealed class SpacyTokenizerNunitTests
    {
        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(SpacyTokenizerSuites.All);
        }

        /// <summary>
        /// Execute a single shared test case.
        /// </summary>
        /// <param name="testCase">Test case to run.</param>
        /// <returns>Task.</returns>
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
