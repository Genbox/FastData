using System.Diagnostics;
using Genbox.FastData.Abstracts;
using Genbox.FastData.ArrayHash;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.Misc;

namespace Genbox.FastData;

public static class FastDataGenerator
{
    public static bool TryGenerate(object[] data, FastDataConfig fdCfg, ICodeGenerator generator, out string? source) => data[0] switch
    {
        bool => TryGenerate(data.Cast<bool>().ToArray(), fdCfg, generator, out source),
        char => TryGenerate(data.Cast<char>().ToArray(), fdCfg, generator, out source),
        sbyte => TryGenerate(data.Cast<sbyte>().ToArray(), fdCfg, generator, out source),
        byte => TryGenerate(data.Cast<byte>().ToArray(), fdCfg, generator, out source),
        short => TryGenerate(data.Cast<short>().ToArray(), fdCfg, generator, out source),
        ushort => TryGenerate(data.Cast<ushort>().ToArray(), fdCfg, generator, out source),
        int => TryGenerate(data.Cast<int>().ToArray(), fdCfg, generator, out source),
        uint => TryGenerate(data.Cast<uint>().ToArray(), fdCfg, generator, out source),
        long => TryGenerate(data.Cast<long>().ToArray(), fdCfg, generator, out source),
        ulong => TryGenerate(data.Cast<ulong>().ToArray(), fdCfg, generator, out source),
        float => TryGenerate(data.Cast<float>().ToArray(), fdCfg, generator, out source),
        double => TryGenerate(data.Cast<double>().ToArray(), fdCfg, generator, out source),
        string => TryGenerate(data.Cast<string>().ToArray(), fdCfg, generator, out source),
        _ => throw new InvalidOperationException("Unsupported data type: " + data[0].GetType().Name)
    };

    public static bool TryGenerate<T>(T[] data, FastDataConfig fdCfg, ICodeGenerator generator, out string? source)
    {
        //Validate that we only have unique data
        HashSet<T> uniq = new HashSet<T>();

        foreach (T val in data)
        {
            if (!uniq.Add(val))
                throw new InvalidOperationException($"Duplicate data found: {val}");
        }

        DataProperties<T> props = DataProperties<T>.Create(data);

        bool analysisEnabled = false;

        IStringHash? spec = null;
        if (data is string[] stringArr)
            spec = analysisEnabled ? GetBestHash(stringArr, props.StringProps!, fdCfg.SimulatorConfig) : new DefaultStringHash();

        HashFunc<T> hashFunc;

        //If we have a string hash, use it.
        if (spec != null)
            hashFunc = (HashFunc<T>)(object)spec.GetHashFunction();
        else
            hashFunc = PrimitiveHash.GetHash<T>(props.DataType);

        IContext? context = null;

        foreach (object candidate in GetDataStructureCandidates(data, fdCfg, props))
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

        GeneratorConfig<T> genCfg = new GeneratorConfig<T>(fdCfg.StructureType, fdCfg.StringComparison, props, spec);
        return generator.TryGenerate(genCfg, context, out source);
    }

    private static IEnumerable<object> GetDataStructureCandidates<T>(T[] data, FastDataConfig config, DataProperties<T> props)
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

                // If it is a string, we try key lengths
                if (props.DataType == DataType.String)
                    yield return new KeyLengthStructure<T>(props.StringProps!);

                // TODO: Attempt perfect hashing
                // yield return new HashSetPerfectStructure<T>();

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                    yield return new BinarySearchStructure<T>(props.DataType, config.StringComparison);
                else
                    yield return new HashSetChainStructure<T>();
            }
        }
        else if (ds == StructureType.Array)
            yield return new ArrayStructure<T>();
        else if (ds == StructureType.Conditional)
            yield return new ConditionalStructure<T>();
        else if (ds == StructureType.BinarySearch)
            yield return new BinarySearchStructure<T>(props.DataType, config.StringComparison);
        else if (ds == StructureType.HashSet)
            yield return new HashSetChainStructure<T>();
        else
            throw new InvalidOperationException($"Unsupported DataStructure {ds}");
    }

    private static IStringHash GetBestHash(string[] data, StringProperties props, SimulatorConfig simConf)
    {
        Simulator sim = new Simulator(data, simConf);

        //Run each of the analyzers
        List<Candidate> candidates = new List<Candidate>();

        BruteForceAnalyzer bf = new BruteForceAnalyzer(props, new BruteForceAnalyzerConfig(), sim);
        if (bf.IsAppropriate())
            candidates.AddRange(bf.GetCandidates());

        GeneticAnalyzer ga = new GeneticAnalyzer(props, new GeneticAnalyzerConfig(), sim);
        if (ga.IsAppropriate())
            candidates.AddRange(ga.GetCandidates());

        GPerfAnalyzer ha = new GPerfAnalyzer(data, props, new GPerfAnalyzerConfig(), sim);
        if (ha.IsAppropriate())
            candidates.AddRange(ha.GetCandidates());

        //Split candidates into perfect and not perfect
        List<Candidate> perfect = new List<Candidate>();
        List<Candidate> notPerfect = new List<Candidate>();

        foreach (Candidate candidate in candidates)
        {
            if (candidate.Collisions == 0)
                perfect.Add(candidate);
            else
                notPerfect.Add(candidate);
        }

        //Sort both on fitness
        perfect.Sort(static (a, b) => a.Fitness.CompareTo(b.Fitness));
        notPerfect.Sort(static (a, b) => a.Fitness.CompareTo(b.Fitness));

        //We start with the perfect results (if any)
        if (perfect.Count > 0)
        {
            foreach (Candidate candidate in perfect)
                Benchmark(data, candidate);

            //Sort by time
            perfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));

            //Take the first non-perfect candidate (highest fitness) and benchmark it too
            if (notPerfect.Count > 0)
            {
                Candidate np = notPerfect[0];
                Benchmark(data, np);

                //If the perfect is faster, we use that one.
                Candidate p = perfect[0];

                if (p.Time <= np.Time)
                    return p.StringHash;

                //If the not-perfect is faster, it has to be so by x% before we pick it over a perfect hash.
                //E.g. we still want the perfect, even if it is x% slower

                double threshold = p.Time + (p.Time * 0.25);

                if (np.Time < threshold)
                    return np.StringHash;

                return p.StringHash;
            }
        }

        //If there are no perfect candidates, we benchmark 10 candidates and return the fastest.
        for (int i = 0; i < 10; i++)
            Benchmark(data, notPerfect[i]);

        notPerfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return notPerfect[0].StringHash;
    }

    private static void Benchmark(string[] data, Candidate candidate)
    {
        //The candidate has already been benchmarked. Do nothing.
        if (candidate.Time >= double.Epsilon)
            return;

        HashFunc<string> func = candidate.StringHash.GetHashFunction();

        //Warmup
        for (int i = 0; i < 10; i++)
        {
            foreach (string s in data)
                func(s);
        }

        Stopwatch sw = new Stopwatch();

        for (int i = 0; i < 10; i++)
        {
            foreach (string s in data)
                func(s);
        }

        sw.Stop();

        candidate.Time = sw.ElapsedTicks / 10.0;
    }
}