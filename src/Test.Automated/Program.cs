namespace Test.Automated
{
    using System;
    using System.Threading.Tasks;

    using Test.Shared;

    using Touchstone.Cli;

    /// <summary>
    /// Touchstone CLI runner for the SpacyTokenizerSdk test suite. Executes every shared
    /// suite defined in <see cref="SpacyTokenizerSuites"/> and returns a non-zero exit code
    /// if any test fails, so it can gate CI pipelines.
    ///
    /// Usage: dotnet run --project Test.Automated [-- --results results.json]
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command-line arguments; "--results &lt;path&gt;" exports JSON results.</param>
        /// <returns>Process exit code: 0 on success, non-zero on any failure.</returns>
        public static async Task<int> Main(string[] args)
        {
            string resultsPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--results" && i + 1 < args.Length)
                {
                    resultsPath = args[i + 1];
                    break;
                }
            }

            return await ConsoleRunner.RunAsync(SpacyTokenizerSuites.All, resultsPath: resultsPath);
        }
    }
}
