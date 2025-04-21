using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData;

public static class FastDataGenerator
{
    public static bool TryGenerate(object[] data, FastDataConfig fdCfg, IGenerator generator, out string? source)
    {
        //Validate that we only have unique data
        HashSet<object> uniq = new HashSet<object>();

        foreach (object o in data)
        {
            if (!uniq.Add(o))
                throw new InvalidOperationException("Duplicate data found: " + o);
        }

        DataProperties props = new DataProperties(data);
        StructureConfig strCfg = new StructureConfig(props, fdCfg.StringComparison);

        foreach (object candidate in GetDataStructureCandidates(data, fdCfg, strCfg))
        {
            if (TryCreateStructure(candidate, data, props, fdCfg, out IHashSpec? hashSpec, out IContext? context))
            {
                GeneratorConfig genCfg = new GeneratorConfig(fdCfg.StructureType, fdCfg.StringComparison, props, hashSpec ?? DefaultHashSpec.Instance);
                return generator.TryGenerate(genCfg, context, out source);
            }
        }

        source = null;
        return false;
    }

    private static IEnumerable<object> GetDataStructureCandidates(object[] data, FastDataConfig config, StructureConfig cfg)
    {
        StructureType ds = config.StructureType;

        if (ds == StructureType.Auto)
        {
            if (data.Length == 1)
                yield return new SingleValueStructure();
            else
            {
                // For small amounts of data, logic is the fastest, so we try that first
                yield return new ConditionalStructure();

                // We try key lengths
                yield return new KeyLengthStructure(cfg);

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory) && config.StorageOptions.HasFlag(StorageOption.OptimizeForSpeed))
                    yield return new PerfectHashBruteForceStructure();

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                    yield return new BinarySearchStructure(cfg);
                else
                    yield return new HashSetChainStructure();
            }
        }
        else if (ds == StructureType.SingleValue)
            yield return new SingleValueStructure();
        else if (ds == StructureType.Array)
            yield return new ArrayStructure();
        else if (ds == StructureType.Conditional)
            yield return new ConditionalStructure();
        else if (ds == StructureType.BinarySearch)
            yield return new BinarySearchStructure(cfg);
        else if (ds == StructureType.EytzingerSearch)
            yield return new EytzingerSearchStructure(cfg);
        else if (ds == StructureType.PerfectHashGPerf)
            yield return new PerfectHashGPerfStructure(cfg);
        else if (ds == StructureType.PerfectHashBruteForce)
            yield return new PerfectHashBruteForceStructure();
        else if (ds == StructureType.HashSetChain)
            yield return new HashSetChainStructure();
        else if (ds == StructureType.HashSetLinear)
            yield return new HashSetLinearStructure();
        else if (ds == StructureType.KeyLength)
            yield return new KeyLengthStructure(cfg);
        else
            throw new InvalidOperationException($"Unsupported DataStructure {ds}");
    }

    private static bool TryCreateStructure(object candidate, object[] data, DataProperties props, FastDataConfig fdCfg, out IHashSpec? hashSpec, out IContext? context)
    {
        if (candidate is IHashStructure hs)
        {
            hashSpec = DefaultHashSpec.Instance;

            if (props.StringProps != null)
            {
                Simulator simulator = new Simulator(data, fdCfg.SimulatorConfig);

                if (fdCfg.AnalyzerConfig is BruteForceAnalyzerConfig bfCfg)
                    hashSpec = new BruteForceAnalyzer(props.StringProps.Value, bfCfg, simulator).Run().Spec;
                else if (fdCfg.AnalyzerConfig is GeneticAnalyzerConfig gaCfg)
                    hashSpec = new GeneticAnalyzer(gaCfg, simulator).Run().Spec;
            }

            return hs.TryCreate(data, hashSpec.GetHashFunction(), out context);
        }

        hashSpec = null;

        if (candidate is IStructure s)
            return s.TryCreate(data, out context);

        context = null;
        return false;
    }
}