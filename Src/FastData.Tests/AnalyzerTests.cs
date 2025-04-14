using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Analyzers.BruteForce;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic;
using Genbox.FastData.Internal.Analysis.Analyzers.Heuristics;
using Genbox.FastData.Internal.Analysis.Properties;

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

        Simulator sim = new Simulator(new SimulatorConfig(), data, (_, _, _, _) => [1]); //We run a fake emulation that just returns 1

        yield return [new BruteForceAnalyzer(props, new BruteForceAnalyzerConfig(), sim)];
        yield return [new GeneticAnalyzer(new GeneticAnalyzerConfig(), sim)];
        yield return [new HeuristicAnalyzer(data, props, new HeuristicAnalyzerConfig(), sim)];
    }
}