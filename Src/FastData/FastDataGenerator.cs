using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.Specs;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData;

public static class FastDataGenerator
{
    public static bool TryGenerate<T>(T[] data, FastDataConfig fdCfg, ICodeGenerator generator, out string? source)
    {
        //Validate that we only have unique data
        HashSet<T> uniq = new HashSet<T>();

        foreach (T val in data)
        {
            if (!uniq.Add(val))
                throw new InvalidOperationException("Duplicate data found: " + val);
        }

        DataProperties props = DataProperties.Create(data);
        StructureConfig strCfg = new StructureConfig(props, fdCfg.StringComparison);

        bool analysisEnabled = false;

        IHashSpec? spec = null;
        if (analysisEnabled && data is string[] stringArr)
            spec = GetHashSpec(stringArr, props);

        HashFunc<T> hashFunc;

        //If we have a hash spec, use it.
        if (spec != null)
            hashFunc = (HashFunc<T>)(object)spec.GetHashFunction();
        else
            hashFunc = static obj => (uint)obj.GetHashCode();

        IContext? context = null;

        foreach (object candidate in GetDataStructureCandidates(data, fdCfg, strCfg))
        {
            if (candidate is IHashStructure<T> hs)
            {
                if (hs.TryCreate(data, hashFunc, out context))
                    break;
            }
            else if (candidate is IStructure<T> s)
                if (s.TryCreate(data, out context))
                    break;
        }

        if (context == null)
            throw new InvalidOperationException("Unable to find a suitable data structure for the data. Please report this as a bug.");

        GeneratorConfig genCfg = new GeneratorConfig(fdCfg.StructureType, fdCfg.StringComparison, props, spec);
        return generator.TryGenerate<T>(genCfg, context, out source);
    }

    private static IEnumerable<object> GetDataStructureCandidates<T>(T[] data, FastDataConfig config, StructureConfig cfg)
    {
        StructureType ds = config.StructureType;

        if (ds == StructureType.Auto)
        {
            if (data.Length == 1)
                yield return new SingleValueStructure<T>();
            else
            {
                // For small amounts of data, logic is the fastest, so we try that first
                yield return new ConditionalStructure<T>();

                // We try key lengths
                yield return new KeyLengthStructure<T>(cfg);

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                    yield return new BinarySearchStructure<T>(cfg);
                else
                    yield return new HashSetChainStructure<T>();
            }
        }
        else if (ds == StructureType.SingleValue)
            yield return new SingleValueStructure<T>();
        else if (ds == StructureType.Array)
            yield return new ArrayStructure<T>();
        else if (ds == StructureType.Conditional)
            yield return new ConditionalStructure<T>();
        else if (ds == StructureType.BinarySearch)
            yield return new BinarySearchStructure<T>(cfg);
        else if (ds == StructureType.EytzingerSearch)
            yield return new EytzingerSearchStructure<T>(cfg);
        else if (ds == StructureType.PerfectHashGPerf)
            yield return new PerfectHashGPerfStructure<T>(cfg);
        else if (ds == StructureType.PerfectHashBruteForce)
            yield return new PerfectHashBruteForceStructure<T>();
        else if (ds == StructureType.HashSetChain)
            yield return new HashSetChainStructure<T>();
        else if (ds == StructureType.HashSetLinear)
            yield return new HashSetLinearStructure<T>();
        else if (ds == StructureType.KeyLength)
            yield return new KeyLengthStructure<T>(cfg);
        else
            throw new InvalidOperationException($"Unsupported DataStructure {ds}");
    }

    private static IHashSpec GetHashSpec(string[] data, DataProperties props)
    {
        //Run each of the analyzers
        Simulator simulator = new Simulator(data, new SimulatorConfig());
        BruteForceAnalyzer bf = new BruteForceAnalyzer(props.StringProps!.Value, new BruteForceAnalyzerConfig(), simulator);
        Candidate<BruteForceHashSpec> bfCand = bf.Run();

        GeneticAnalyzer ga = new GeneticAnalyzer(new GeneticAnalyzerConfig(), simulator);
        Candidate<GeneticHashSpec> gaCand = ga.Run();

        HeuristicAnalyzer ha = new HeuristicAnalyzer(data, props.StringProps!.Value, new HeuristicAnalyzerConfig(), simulator);
        Candidate<HeuristicHashSpec> haCand = ha.Run();

        //Select the spec with the best fitness
        return bfCand.Fitness >= gaCand.Fitness ? bfCand.Fitness >= haCand.Fitness ? bfCand.Spec : haCand.Spec :
            gaCand.Fitness >= haCand.Fitness ? gaCand.Spec : haCand.Spec;
    }
}