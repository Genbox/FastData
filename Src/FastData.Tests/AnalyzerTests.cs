using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.BruteForce;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Tests;

public class AnalyzerTests
{
    private static readonly object[] _data = ["aa"];

    [Fact]
    public void BruteForceAnalyzerTest()
    {
        StringProperties props = DataAnalyzer.GetStringProperties(_data);
        BruteForceAnalyzer analyzer = new BruteForceAnalyzer(_data, props, new BruteForceSettings(), Simulation);

        Candidate<BruteForceHashSpec> top = analyzer.Run();
        Assert.Equal(1, top.Fitness);
        return;

        void Simulation(object[] data, BruteForceSettings settings, ref Candidate<BruteForceHashSpec> candidate) => candidate.Fitness = 1;
    }

    [Theory]
    [InlineData(HashFunction.DJB2Hash, 1853903583)]
    [InlineData(HashFunction.XxHash, 1713611331)]
    internal void BFHashSpecTest(HashFunction function, uint vector)
    {
        BruteForceHashSpec spec = new BruteForceHashSpec(function, [new StringSegment(0, -1, Alignment.Left)]);
        Func<string, uint> func = spec.GetFunction();
        Assert.Equal(vector, func("hello world"));

        //The source code must give the same result
        string source = spec.GetSource();
        Func<string, uint> func2 = CodeGenerator.GetDelegate<Func<string, uint>>(source, true);
        Assert.Equal(vector, func2("hello world"));
    }

    [Fact]
    public void GeneticAnalyzerTest()
    {
        StringProperties props = DataAnalyzer.GetStringProperties(_data);
        GeneticAnalyzer analyzer = new GeneticAnalyzer(_data, props, new GeneticSettings(), Simulation);

        Candidate<GeneticHashSpec> top = analyzer.Run();
        Assert.Equal(1, top.Fitness);
        return;

        void Simulation(object[] data, GeneticSettings settings, ref Candidate<GeneticHashSpec> candidate) => candidate.Fitness = 1;
    }

    [Theory]
    [InlineData(1, 1, 2138145203)]
    [InlineData(2, 2, 401880771)]
    internal void GeneticHashSpecTest(int mixerSeed, int avalancheSeed, uint vector)
    {
        GeneticHashSpec spec = new GeneticHashSpec(mixerSeed, 1, avalancheSeed, 1, [new StringSegment(0, -1, Alignment.Left)]);
        Func<string, uint> func = spec.GetFunction();
        Assert.Equal(vector, func("hello world"));

        //The source code must give the same result
        string source = spec.GetSource();
        Func<string, uint> func2 = CodeGenerator.GetDelegate<Func<string, uint>>(source, true);
        Assert.Equal(vector, func2("hello world"));
    }
}