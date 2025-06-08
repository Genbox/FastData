using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData;

[SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters")]
public static partial class FastDataGenerator
{
    internal const StringComparison DefaultStringComparison = StringComparison.Ordinal;

    public static bool TryGenerate(object[] data, FastDataConfig fdCfg, ICodeGenerator generator, out string? source, ILoggerFactory? factory = null) => data[0] switch
    {
        char => TryGenerate(data.Cast<char>().ToArray(), fdCfg, generator, out source, factory),
        sbyte => TryGenerate(data.Cast<sbyte>().ToArray(), fdCfg, generator, out source, factory),
        byte => TryGenerate(data.Cast<byte>().ToArray(), fdCfg, generator, out source, factory),
        short => TryGenerate(data.Cast<short>().ToArray(), fdCfg, generator, out source, factory),
        ushort => TryGenerate(data.Cast<ushort>().ToArray(), fdCfg, generator, out source, factory),
        int => TryGenerate(data.Cast<int>().ToArray(), fdCfg, generator, out source, factory),
        uint => TryGenerate(data.Cast<uint>().ToArray(), fdCfg, generator, out source, factory),
        long => TryGenerate(data.Cast<long>().ToArray(), fdCfg, generator, out source, factory),
        ulong => TryGenerate(data.Cast<ulong>().ToArray(), fdCfg, generator, out source, factory),
        float => TryGenerate(data.Cast<float>().ToArray(), fdCfg, generator, out source, factory),
        double => TryGenerate(data.Cast<double>().ToArray(), fdCfg, generator, out source, factory),
        string => TryGenerate(data.Cast<string>().ToArray(), fdCfg, generator, out source, factory),
        _ => throw new InvalidOperationException($"Unsupported data type: {data[0].GetType().Name}")
    };

    public static bool TryGenerate<T>(T[] data, FastDataConfig fdCfg, ICodeGenerator generator, out string? source, ILoggerFactory? factory = null) where T : notnull
    {
        if (data.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (!IsValidType<T>())
            throw new InvalidOperationException($"Unsupported data type: {typeof(T).Name}");

        factory ??= NullLoggerFactory.Instance;

        //Validate that we only have unique data
        HashSet<T> uniq = new HashSet<T>();

        foreach (T val in data)
        {
            if (!uniq.Add(val))
                throw new InvalidOperationException($"Duplicate data found: {val}");
        }

        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));
        LogUniqueItems(logger, uniq.Count);

        DataProperties<T> props = DataProperties<T>.Create(data);

        LogDataType(logger, props.DataType);

        if (props.FloatProps != null)
            LogMinMaxValues(logger, props.FloatProps.MinValue, props.FloatProps.MaxValue);
        else if (props.IntProps != null)
            LogMinMaxValues(logger, props.IntProps.MinValue, props.IntProps.MaxValue);
        else if (props.StringProps != null)
            LogMinMaxLength(logger, props.StringProps.LengthData.Min, props.StringProps.LengthData.Max);

        IContext? context = null;

        LogUserStructureType(logger, fdCfg.StructureType);
        foreach (IStructure<T>? candidate in GetDataStructureCandidates(data, fdCfg, props))
        {
            LogCandidateAttempt(logger, candidate.GetType().Name);

            if (candidate.TryCreate(data, out context))
            {
                LogCandidateSuccess(logger, candidate.GetType().Name);
                break;
            }

            LogCandidateFailed(logger, candidate.GetType().Name);
        }

        if (context == null)
            throw new InvalidOperationException("Unable to find a suitable data structure for the data. Please report this as a bug.");

        GeneratorConfig<T> genCfg = new GeneratorConfig<T>(fdCfg.StructureType, DefaultStringComparison, props);
        return generator.TryGenerate(genCfg, context, out source);
    }

    private static bool IsValidType<T>()
    {
        Type type = typeof(T);

        return type == typeof(char) || type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double) || type == typeof(string);
    }

    private static IEnumerable<IStructure<T>> GetDataStructureCandidates<T>(T[] data, FastDataConfig config, DataProperties<T> props)
    {
        StructureType ds = config.StructureType;

        if (ds == StructureType.Auto)
        {
            if (data.Length == 1)
                yield return new SingleValueStructure<T>();
            else
            {
                // For small amounts of data, logic is the fastest.
                // However, it increases the assembly size, so we want to try some special cases first.

                // If it is string data, and we have unique lengths, we prefer to use a KeyLengthStructure.
                if (props.DataType == DataType.String && props.StringProps!.LengthData.Unique)
                    yield return new KeyLengthStructure<T>(props.StringProps!);

                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (data.Length < 400)
                    yield return new ConditionalStructure<T>();

                HashData hashData = HashData.Create(data, props.DataType, config.HashCapacityFactor);

                if (hashData.HashCodesPerfect)
                    yield return new HashSetPerfectStructure<T>(hashData);

                yield return new HashSetChainStructure<T>(hashData);
            }
        }
        else if (ds == StructureType.Array)
            yield return new ArrayStructure<T>();
        else if (ds == StructureType.Conditional)
            yield return new ConditionalStructure<T>();
        else if (ds == StructureType.BinarySearch)
            yield return new BinarySearchStructure<T>(props.DataType, DefaultStringComparison);
        else if (ds == StructureType.HashSet)
            yield return new HashSetChainStructure<T>(HashData.Create(data, props.DataType, config.HashCapacityFactor));
        else
            throw new InvalidOperationException($"Unsupported DataStructure {ds}");
    }

    private static IStringHash GetBestHash(string[] data, StringProperties props, SimulatorConfig simConf, ILoggerFactory factory)
    {
        Simulator sim = new Simulator(data, simConf);

        //Run each of the analyzers
        List<Candidate> candidates = new List<Candidate>();

        BruteForceAnalyzer bf = new BruteForceAnalyzer(props, new BruteForceAnalyzerConfig(), sim, factory.CreateLogger<BruteForceAnalyzer>());
        if (bf.IsAppropriate())
            candidates.AddRange(bf.GetCandidates());

        GeneticAnalyzer ga = new GeneticAnalyzer(props, new GeneticAnalyzerConfig(), sim, factory.CreateLogger<GeneticAnalyzer>());
        if (ga.IsAppropriate())
            candidates.AddRange(ga.GetCandidates());

        GPerfAnalyzer ha = new GPerfAnalyzer(data, props, new GPerfAnalyzerConfig(), sim, factory.CreateLogger<GPerfAnalyzer>());
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
            {
                Benchmark(data, candidate);
            }

            //Sort by time
            perfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));

            //Take the first non-perfect candidate (the highest fitness) and benchmark it too
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
        {
            Benchmark(data, notPerfect[i]);
        }

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
            {
                func(s);
            }
        }

        Stopwatch sw = new Stopwatch();

        for (int i = 0; i < 10; i++)
        {
            foreach (string s in data)
            {
                func(s);
            }
        }

        sw.Stop();

        candidate.Time = sw.ElapsedTicks / 10.0;
    }
}