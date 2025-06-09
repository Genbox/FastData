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

    /// <summary>Generate source code for the provided data.</summary>
    /// <param name="data">The data to generate from. Note that all objects must be the same type.</param>
    /// <param name="fdCfg">The configuration to use.</param>
    /// <param name="generator">The code generator to use.</param>
    /// <param name="factory">A logging factory</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when you gave an unsupported data type in data.</exception>
    public static string Generate(object[] data, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return Generate(new ReadOnlySpan<object>(data), fdCfg, generator, factory);
    }

    /// <summary>Generate source code for the provided data.</summary>
    /// <param name="data">The data to generate from. Note that all objects must be the same type.</param>
    /// <param name="fdCfg">The configuration to use.</param>
    /// <param name="generator">The code generator to use.</param>
    /// <param name="factory">A logging factory</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when you gave an unsupported data type in data.</exception>
    public static string Generate(ReadOnlySpan<object> data, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null) => data[0] switch
    {
        char => Generate(Cast<char>(data), fdCfg, generator, factory),
        sbyte => Generate(Cast<sbyte>(data), fdCfg, generator, factory),
        byte => Generate(Cast<byte>(data), fdCfg, generator, factory),
        short => Generate(Cast<short>(data), fdCfg, generator, factory),
        ushort => Generate(Cast<ushort>(data), fdCfg, generator, factory),
        int => Generate(Cast<int>(data), fdCfg, generator, factory),
        uint => Generate(Cast<uint>(data), fdCfg, generator, factory),
        long => Generate(Cast<long>(data), fdCfg, generator, factory),
        ulong => Generate(Cast<ulong>(data), fdCfg, generator, factory),
        float => Generate(Cast<float>(data), fdCfg, generator, factory),
        double => Generate(Cast<double>(data), fdCfg, generator, factory),
        string => Generate(Cast<string>(data), fdCfg, generator, factory),
        _ => throw new InvalidOperationException($"Unsupported data type: {data[0].GetType().Name}")
    };

    /// <summary>Generate source code for the provided data.</summary>
    /// <param name="data">The data to generate from.</param>
    /// <param name="fdCfg">The configuration to use.</param>
    /// <param name="generator">The code generator to use.</param>
    /// <param name="factory">A logging factory</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when you gave an unsupported data type in data.</exception>
    public static string Generate<T>(T[] data, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null) where T : notnull
    {
        return Generate(new ReadOnlySpan<T>(data), fdCfg, generator, factory);
    }

    /// <summary>Generate source code for the provided data.</summary>
    /// <param name="data">The data to generate from.</param>
    /// <param name="fdCfg">The configuration to use.</param>
    /// <param name="generator">The code generator to use.</param>
    /// <param name="factory">A logging factory</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when you gave an unsupported data type in data.</exception>
    public static string Generate<T>(ReadOnlySpan<T> data, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null) where T : notnull
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

        LogUserStructureType(logger, fdCfg.StructureType);
        GeneratorConfig<T> genCfg = new GeneratorConfig<T>(fdCfg.StructureType, DefaultStringComparison, props);

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (data.Length == 1)
                    return Generate(generator, genCfg, new SingleValueStructure<T>(), data);

                // For small amounts of data, logic is the fastest.
                // However, it increases the assembly size, so we want to try some special cases first.
                // If it is string data, and we have unique lengths, we prefer to use a KeyLengthStructure.
                if (props.DataType == DataType.String && props.StringProps!.LengthData.Unique)
                    return Generate(generator, genCfg, new KeyLengthStructure<T>(props.StringProps), data);

                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (data.Length < 400)
                    return Generate(generator, genCfg, new ConditionalStructure<T>(), data);

                HashData hashData = HashData.Create(data, props.DataType, fdCfg.HashCapacityFactor);

                if (hashData.HashCodesPerfect)
                    return Generate(generator, genCfg, new HashSetPerfectStructure<T>(hashData), data);

                return Generate(generator, genCfg, new HashSetChainStructure<T>(hashData), data);
            }
            case StructureType.Array:
                return Generate(generator, genCfg, new ArrayStructure<T>(), data);
            case StructureType.Conditional:
                return Generate(generator, genCfg, new ConditionalStructure<T>(), data);
            case StructureType.BinarySearch:
                return Generate(generator, genCfg, new BinarySearchStructure<T>(props.DataType, DefaultStringComparison), data);
            case StructureType.HashSet:
            {
                HashData hashData = HashData.Create(data, props.DataType, fdCfg.HashCapacityFactor);

                if (hashData.HashCodesPerfect)
                    return Generate(generator, genCfg, new HashSetPerfectStructure<T>(hashData), data);

                return Generate(generator, genCfg, new HashSetChainStructure<T>(hashData), data);
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static string Generate<T, TContext>(ICodeGenerator generator, GeneratorConfig<T> genCfg, IStructure<T, TContext> structure, ReadOnlySpan<T> data) where TContext : IContext<T>
    {
        TContext res = structure.Create(data);
        return generator.Generate(data, genCfg, res);
    }

    private static bool IsValidType<T>()
    {
        Type type = typeof(T);
        return type == typeof(char) || type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double) || type == typeof(string);
    }

    private static ReadOnlySpan<T> Cast<T>(this ReadOnlySpan<object> data) where T : notnull
    {
        T[] newArr = new T[data.Length];

        for (int i = 0; i < data.Length; i++)
            newArr[i] = (T)data[i];

        return newArr;
    }

    private static IStringHash GetBestHash(string[] data, StringProperties props, SimulatorConfig simConf, ILoggerFactory factory)
    {
        Simulator sim = new Simulator(simConf);

        //Run each of the analyzers
        List<Candidate> candidates = new List<Candidate>();

        BruteForceAnalyzer bf = new BruteForceAnalyzer(props, new BruteForceAnalyzerConfig(), sim, factory.CreateLogger<BruteForceAnalyzer>());
        if (bf.IsAppropriate())
            bf.GetCandidates(data, OnCandidateFound);

        GeneticAnalyzer ga = new GeneticAnalyzer(props, new GeneticAnalyzerConfig(), sim, factory.CreateLogger<GeneticAnalyzer>());
        if (ga.IsAppropriate())
            ga.GetCandidates(data, OnCandidateFound);

        GPerfAnalyzer ha = new GPerfAnalyzer(data.Length, props, new GPerfAnalyzerConfig(), sim, factory.CreateLogger<GPerfAnalyzer>());
        if (ha.IsAppropriate())
            ha.GetCandidates(data, OnCandidateFound);

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

        bool OnCandidateFound(Candidate cand)
        {
            candidates.Add(cand);
            return true;
        }
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