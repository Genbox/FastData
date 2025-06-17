using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
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
        LogUserStructureType(logger, fdCfg.StructureType);
        LogUniqueItems(logger, uniq.Count);

        DataType dataType = (DataType)Enum.Parse(typeof(DataType), typeof(T).Name);
        LogDataType(logger, dataType);

        GeneratorConfig<T> genCfg;
        IProperties props;
        HashDetails hashDetails = new HashDetails();

        if (dataType == DataType.String)
        {
            StringProperties strProps = DataAnalyzer.GetStringProperties(data);
            LogMinMaxLength(logger, strProps.LengthData.Min, strProps.LengthData.Max);
            genCfg = new GeneratorConfig<T>(fdCfg.StructureType, dataType, (uint)data.Length, strProps, DefaultStringComparison, hashDetails);
            props = strProps;
        }
        else
        {
            ValueProperties<T> valProps = DataAnalyzer.GetValueProperties(data, dataType);
            LogMinMaxValues(logger, valProps.MinValue, valProps.MaxValue);
            genCfg = new GeneratorConfig<T>(fdCfg.StructureType, dataType, (uint)data.Length, valProps, hashDetails);
            props = valProps;
        }

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (data.Length == 1)
                    return Generate(generator, genCfg, new SingleValueStructure<T>(), data);

                // For small amounts of data, logic is the fastest. However, it increases the assembly size, so we want to try some special cases first.

                // If strings have unique lengths, we prefer to use a KeyLengthStructure.
                if (props is StringProperties strProps && strProps.LengthData.Unique)
                    return Generate(generator, genCfg, new KeyLengthStructure<T>(strProps), data);

                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (data.Length < 400)
                    return Generate(generator, genCfg, new ConditionalStructure<T>(), data);

                goto case StructureType.HashSet;
            }
            case StructureType.Array:
                return Generate(generator, genCfg, new ArrayStructure<T>(), data);
            case StructureType.Conditional:
                return Generate(generator, genCfg, new ConditionalStructure<T>(), data);
            case StructureType.BinarySearch:
                return Generate(generator, genCfg, new BinarySearchStructure<T>(dataType, DefaultStringComparison), data);
            case StructureType.HashSet:
            {
                HashFunc<T> hashFunc;

                if (props is StringProperties strProps)
                {
                    if (fdCfg.StringAnalyzerConfig != null)
                    {
                        IStringHash stringHash = GetBestHash(data, strProps, fdCfg.StringAnalyzerConfig, factory, true).StringHash;
                        hashFunc = (HashFunc<T>)(object)stringHash.GetHashFunction();
                        hashDetails.StringHash = new StringHashDetails(stringHash.GetExpression(), stringHash.Functions, stringHash.State);
                    }
                    else
                    {
                        DefaultStringHash stringHash = new DefaultStringHash();
                        hashFunc = (HashFunc<T>)(object)stringHash.GetHashFunction();

                        //We do not set hashDetails.StringHash here, as the user requested it to be disabled
                    }
                }
                else if (props is ValueProperties<T> valueProps)
                {
                    hashFunc = PrimitiveHash.GetHash<T>(dataType, valueProps.HasZeroOrNaN);
                    hashDetails.HasZeroOrNaN = valueProps.HasZeroOrNaN;
                }
                else
                    throw new InvalidOperationException("Bug");

                HashData hashData = HashData.Create(data, fdCfg.HashCapacityFactor, hashFunc);

                if (hashData.HashCodesPerfect)
                    return Generate(generator, genCfg, new HashSetPerfectStructure<T>(hashData, dataType), data);

                return Generate(generator, genCfg, new HashSetChainStructure<T>(hashData, dataType), data);
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static string Generate<T, TContext>(ICodeGenerator generator, GeneratorConfig<T> genCfg, IStructure<T, TContext> structure, ReadOnlySpan<T> data) where T : notnull where TContext : IContext<T>
    {
        TContext res = structure.Create(ref data);
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

    internal static Candidate GetBestHash<T>(ReadOnlySpan<T> data, StringProperties props, StringAnalyzerConfig cfg, ILoggerFactory factory, bool includeDefault) where T : notnull
    {
        Simulator<T> sim = new Simulator<T>(data.Length);

        //Run each of the analyzers
        List<Candidate> candidates = new List<Candidate>();

        //We always add the default hash as a candidate
        if (includeDefault)
            candidates.Add(sim.Run(data, new DefaultStringHash()));

        if (cfg.BruteForceAnalyzerConfig != null)
        {
            BruteForceAnalyzer<T> bf = new BruteForceAnalyzer<T>(props, cfg.BruteForceAnalyzerConfig, sim, factory.CreateLogger<BruteForceAnalyzer<T>>());
            if (bf.IsAppropriate())
                candidates.AddRange(bf.GetCandidates(data));
        }

        if (cfg.GeneticAnalyzerConfig != null)
        {
            GeneticAnalyzer<T> ga = new GeneticAnalyzer<T>(props, cfg.GeneticAnalyzerConfig, sim, factory.CreateLogger<GeneticAnalyzer<T>>());
            if (ga.IsAppropriate())
                candidates.AddRange(ga.GetCandidates(data));
        }

        if (cfg.GPerfAnalyzerConfig != null)
        {
            GPerfAnalyzer<T> ha = new GPerfAnalyzer<T>(data.Length, props, cfg.GPerfAnalyzerConfig, sim, factory.CreateLogger<GPerfAnalyzer<T>>());
            if (ha.IsAppropriate())
                candidates.AddRange(ha.GetCandidates(data));
        }

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
        perfect.Sort(static (a, b) => b.Fitness.CompareTo(a.Fitness));
        notPerfect.Sort(static (a, b) => b.Fitness.CompareTo(a.Fitness));

        string test = new string('a', (int)props.LengthData.Max);

        //We start with the perfect results (if any)
        if (perfect.Count > 0)
        {
            foreach (Candidate candidate in perfect)
                Benchmark(test, cfg.BenchmarkIterations, candidate);

            //Sort by time
            perfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));

            //Take the first non-perfect candidate (the highest fitness) and benchmark it too
            if (notPerfect.Count > 0)
            {
                Candidate np = notPerfect[0];
                Benchmark(test, cfg.BenchmarkIterations, np);

                //If the perfect is faster, we use that one.
                Candidate p = perfect[0];

                if (p.Time <= np.Time)
                    return p;

                //If the not-perfect is faster, it has to be so by 25% before we pick it over a perfect hash.
                //E.g. we still want the perfect, even if it is 25% slower

                double threshold = p.Time + (p.Time * 0.25);

                if (np.Time < threshold)
                    return np;

                return p;
            }

            return perfect[0];
        }

        //If there are no perfect candidates, we benchmark all the not-perfect candidates
        foreach (Candidate candidate in notPerfect)
            Benchmark(test, cfg.BenchmarkIterations, candidate);

        notPerfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return notPerfect[0];
    }

    private static void Benchmark(string str, int iterations, Candidate candidate)
    {
        //The candidate has already been benchmarked. Do nothing.
        if (candidate.Time >= double.Epsilon)
            return;

        HashFunc<string> func = candidate.StringHash.GetHashFunction();

        //Warmup
        for (int i = 0; i < iterations; i++)
            func(str);

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
            func(str);

        sw.Stop();

        candidate.Time = sw.ElapsedTicks / (double)iterations;
    }
}