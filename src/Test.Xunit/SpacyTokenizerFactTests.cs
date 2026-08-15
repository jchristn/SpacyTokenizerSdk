namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Test.Shared;

    using Touchstone.Core;
    using Touchstone.XunitAdapter;

    using global::Xunit;

    /// <summary>
    /// Fact-style xUnit host via the Touchstone xUnit adapter base class. Runs every shared
    /// suite inside a single fact, aggregating failures. Complements
    /// <see cref="SpacyTokenizerTheoryTests"/>, which reports one result per case.
    /// </summary>
    public sealed class SpacyTokenizerFactTests : TouchstoneFactBase
    {
        /// <summary>
        /// Shared suites executed by the base class.
        /// </summary>
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return SpacyTokenizerSuites.All; }
        }

        /// <summary>
        /// Execute all shared suites.
        /// </summary>
        /// <returns>Task.</returns>
        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
