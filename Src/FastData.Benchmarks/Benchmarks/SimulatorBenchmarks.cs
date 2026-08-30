using System.Linq.Expressions;
using BenchmarkDotNet.Order;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class SimulatorBenchmarks
{
    private BruteForceStringHash _bruteForceHash = null!;
    private string[] _keys = null!;
    private StringKeyProperties _properties = null!;
    private Simulator _simulator = null!;
    private IStringHash _stringHash = null!;

    [Params(32, 256, 1000, 10000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keys = new string[Count];

        for (int i = 0; i < _keys.Length; i++)
            _keys[i] = $"key-{i:D8}-abcdefghijklmnopqrstuvwxyz";

        _stringHash = DefaultStringHash.GetInstance(GeneratorEncoding.Utf8Bytes);
        _properties = KeyAnalyzer.GetStringProperties(_keys, false, GeneratorEncoding.Utf8Bytes);
        _bruteForceHash = new BruteForceStringHash(new ArraySegment(0, 8, Alignment.Left), Mix, static hash => hash);
        _simulator = new Simulator(_keys, GeneratorEncoding.Utf8Bytes);

        if (_simulator.Run(_stringHash).Collisions < 0)
            throw new InvalidOperationException("Unexpected collision count.");
    }

    [Benchmark]
    public int Run() => _simulator.Run(_stringHash).Collisions;

    [Benchmark]
    public int RunWithFitness() => _simulator.Run(_bruteForceHash, expression => FitnessHelper.CalculateFitness(_properties, _bruteForceHash.Segment, expression)).Collisions;

    private static BinaryExpression Mix(Expression hash, Expression read) => Expression.Add(Expression.Multiply(hash, Expression.Constant(131UL)), read);
}