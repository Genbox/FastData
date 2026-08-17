using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class GeneticAnalyzerBenchmarks
{
    private readonly string[] _data;
    private readonly GeneticAnalyzerConfig _geneticConfig;
    private readonly StringKeyProperties _properties;
    private readonly SegmentGeneratorConfig _segmentConfig;
    private readonly Simulator _simulator;

    public GeneticAnalyzerBenchmarks()
    {
        _data = new string[32];

        for (int i = 0; i < _data.Length; i++)
            _data[i] = $"key-{i:D8}-abcdefghijklmnopqrstuvwxyz";

        _geneticConfig = new GeneticAnalyzerConfig { MaxGenerations = 10, PopulationSize = 32, RandomSeed = 42 };
        _segmentConfig = new SegmentGeneratorConfig();
        _properties = KeyAnalyzer.GetStringProperties(_data, false, GeneratorEncoding.Utf8Bytes);
        _simulator = new Simulator(_data, GeneratorEncoding.Utf8Bytes);
    }

    [Benchmark]
    public int GetCandidates()
    {
        GeneticAnalyzer analyzer = new GeneticAnalyzer(_properties, _geneticConfig, _segmentConfig, _simulator, NullLogger<GeneticAnalyzer>.Instance);
        int count = 0;

        foreach (Candidate candidate in analyzer.GetCandidates(_data))
            count += candidate.Collisions + 1;

        return count;
    }
}