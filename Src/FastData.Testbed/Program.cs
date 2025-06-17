using Genbox.FastData.Testbed.Tests;

namespace Genbox.FastData.Testbed;

internal static class Program
{
    private static void Main()
    {
        AnalysisTest.TestBest();

        // AnalysisTest.TestNoAnalyzer();
        // AnalysisTest.TestGeneticAnalyzer();
        // AnalysisTest.TestBruteForceAnalyzer();
        // AnalysisTest.TestGPerfAnalyzer();
    }
}