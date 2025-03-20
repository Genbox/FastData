using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.Techniques.BruteForce;
using Genbox.FastData.Internal.Analysis.Techniques.Genetic;
using Genbox.FastData.Internal.Analysis.Techniques.Heuristics;

namespace Genbox.FastData.Tests;

public class AnalyzerTests
{
    /// <summary>Tests if analyzers actually run the simulation</summary>
    [Theory]
    [MemberData(nameof(GetSpecs))]
    internal void EnsureAnalyzerRunsSimulation<T>(IHashAnalyzer<T> analyzer) where T : struct, IHashSpec
    {
        Candidate<T> res = analyzer.Run();
        Assert.Equal(1, res.Fitness);
    }

    public static IEnumerable<object[]> GetSpecs()
    {
        object[] data = ["data"];
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        yield return [new BruteForceAnalyzer(data, props, new BruteForceAnalyzerConfig(), (object[] _, BruteForceAnalyzerConfig _, ref Candidate<BruteForceHashSpec> c) => c.Fitness = 1)];
        yield return [new GeneticAnalyzer(data, props, new GeneticAnalyzerConfig(), (object[] _, GeneticAnalyzerConfig _, ref Candidate<GeneticHashSpec> c) => c.Fitness = 1)];
        yield return [new HeuristicAnalyzer(data, props, new HeuristicAnalyzerConfig(), (object[] _, HeuristicAnalyzerConfig _, ref Candidate<HeuristicHashSpec> c) => c.Fitness = 1)];
    }
}