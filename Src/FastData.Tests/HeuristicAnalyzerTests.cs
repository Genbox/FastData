using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.InternalShared;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Tests;

public class HeuristicAnalyzerTests
{
    [Theory]
    [MemberData(nameof(GenerateTests))]
    public async Task GenericTest(int value)
    {
        Random rng = new Random(42);
        string[] data = Enumerable.Range(0, value).Select(x => TestHelper.GenerateRandomString(rng, value)).ToArray();
        StringProperties props = DataAnalyzer.GetStringProperties(data);

        Simulator s = new Simulator(new SimulatorConfig());
        HeuristicAnalyzer a = new HeuristicAnalyzer(data, props, new HeuristicAnalyzerConfig(), s);

        Candidate<HeuristicStringHash> res = a.Run();

        await Verify(res.Spec.Positions)
              .UseFileName("Test" + value)
              .UseDirectory("Analyzers")
              .DisableDiff();
    }

    public static TheoryData<int> GenerateTests()
    {
        TheoryData<int> data = new TheoryData<int>();

        for (int i = 2; i < 100; i++)
            data.Add(i);

        return data;
    }
}