namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;

    using Test.Shared;

    using Touchstone.Core;

    using global::Xunit;
    using global::Xunit.Abstractions;

    /// <summary>
    /// xUnit host for the shared SpacyTokenizerSdk suites. Each non-skipped Touchstone
    /// descriptor becomes an individual theory row so failures are reported per test case.
    /// The test logic itself lives once in <see cref="SpacyTokenizerSuites"/>.
    /// </summary>
    public sealed class SpacyTokenizerTheoryTests
    {
        private readonly ITestOutputHelper _Output;

        /// <summary>
        /// Initialize with the xUnit output helper.
        /// </summary>
        /// <param name="output">Test output helper.</param>
        public SpacyTokenizerTheoryTests(ITestOutputHelper output)
        {
            _Output = output;
        }

        /// <summary>
        /// Non-skipped cases exposed as theory rows.
        /// </summary>
        /// <returns>Theory data.</returns>
        public static TheoryData<TestCaseDescriptor> TestCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in SpacyTokenizerSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (!testCase.Skip) data.Add(testCase);
                }
            }

            return data;
        }

        /// <summary>
        /// Skipped cases exposed as theory rows (reported via xUnit's skip mechanism).
        /// </summary>
        /// <returns>Theory data.</returns>
        public static TheoryData<TestCaseDescriptor> SkippedCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in SpacyTokenizerSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (testCase.Skip) data.Add(testCase);
                }
            }

            return data;
        }

        /// <summary>
        /// Execute a single shared test case.
        /// </summary>
        /// <param name="testCase">Test case to run.</param>
        /// <returns>Task.</returns>
        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            _Output.WriteLine(testCase.TestId + ": " + testCase.DisplayName);
            await testCase.ExecuteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Skipped cases surfaced through xUnit's skip mechanism.
        /// </summary>
        /// <param name="testCase">Skipped test case.</param>
        /// <returns>Task.</returns>
        [Theory(Skip = "Dynamically skipped test cases")]
        [MemberData(nameof(SkippedCases))]
        public Task Skipped(TestCaseDescriptor testCase)
        {
            _Output.WriteLine("Skipped: " + testCase.SkipReason);
            return Task.CompletedTask;
        }
    }
}
