using System.Diagnostics;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.BruteForce;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Generators;

internal sealed class HashSetCode(FastDataConfig config, GeneratorContext context, HashSetCode.HashSetType hashSetType) : ICode
{
    private IHashSpec? _hashSpec;
    private readonly IHashSetBase _impl = hashSetType == HashSetType.Chain ? new HashSetChain(config, context) : new HashSetLinear(config, context);

    public bool TryCreate()
    {
        if (config.DataType == KnownDataType.String)
        {
            if (config.AnalyzerConfig is BruteForceAnalyzerConfig bfCfg)
            {
                DataProperties props = context.GetDataProperties();
                BruteForceAnalyzer analyzer = new BruteForceAnalyzer(config.Data, props.StringProps.Value, bfCfg, RunSimulation);
                _hashSpec = analyzer.Run().Spec;
                _impl.Create(x => _hashSpec.GetFunction()((string)x));
                return true;
            }
            if (config.AnalyzerConfig is GeneticAnalyzerConfig gaCfg)
            {
                DataProperties props = context.GetDataProperties();
                GeneticAnalyzer analyzer = new GeneticAnalyzer(config.Data, props.StringProps.Value, gaCfg, RunSimulation);
                _hashSpec = analyzer.Run().Spec;
                _impl.Create(x => _hashSpec.GetFunction()((string)x));
                return true;
            }
        }

        _impl.Create(HashHelper.HashObject);
        return true;
    }

    public string Generate() => _impl.Generate(_hashSpec);

    internal static void RunSimulation<T>(object[] data, AnalyzerConfig config, ref Candidate<T> candidate) where T : struct, IHashSpec
    {
        // Generate a hash function from the spec
        Func<string, uint> hashFunc = candidate.Spec.GetFunction();

        int capacity = (int)(data.Length * config.CapacityFactor);
        string first = (string)data[0];

        long ticks = Stopwatch.GetTimestamp();

        //Set power plan to high performance
        //Pin process to 1 core
        //Set process priority to above normal
        for (int i = 0; i < 1000; i++)
            hashFunc(first);
        ticks = Stopwatch.GetTimestamp() - ticks;

        (int occupied, double minVariance, double maxVariance) = Emulate(data, capacity, hashFunc);

        double normOccu = (occupied / (double)capacity) * config.FillWeight;
        double normTime = (1.0 / (1.0 + ((double)ticks / 1000))) * config.TimeWeight;

        candidate.Fitness = (normOccu + normTime) / 2;
        candidate.Metadata = [("Time/norm", ticks + "/" + normTime.ToString("N2")), ("Occupied/norm", occupied + "/" + normOccu.ToString("N2")), ("MinVariance", minVariance), ("MaxVariance", maxVariance)];
    }

    private static (int cccupied, double minVariance, double maxVariance) Emulate(object[] data, int capacity, Func<string, uint> hashFunc)
    {
        int[] buckets = new int[capacity];

        for (int i = 0; i < capacity; i++)
            buckets[hashFunc((string)data[i]) % buckets.Length]++;

        int occupied = 0;
        double minVariance = double.MaxValue;
        double maxVariance = double.MinValue;

        for (int i = 0; i < buckets.Length; i++)
        {
            int bucket = buckets[i];

            if (bucket > 0)
                occupied++;

            minVariance = Math.Min(minVariance, bucket);
            maxVariance = Math.Max(maxVariance, bucket);
        }

        return (occupied, minVariance, maxVariance);
    }

    internal enum HashSetType : byte
    {
        Unknown = 0,
        Chain,
        Linear
    }

    internal interface IHashSetBase
    {
        void Create(Func<object, uint> hashFunc);
        string Generate(IHashSpec? spec);
    }
}