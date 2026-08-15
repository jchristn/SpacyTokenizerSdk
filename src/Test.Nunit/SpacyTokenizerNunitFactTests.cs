namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Test.Shared;

    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Single-test NUnit host via the Touchstone NUnit adapter base class. Runs every shared
    /// suite inside one test, aggregating failures. Complements
    /// <see cref="SpacyTokenizerNunitTests"/>, which reports one result per case.
    /// </summary>
    [TestFixture]
    public sealed class SpacyTokenizerNunitFactTests : TouchstoneNunitBase
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
        [Test]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
