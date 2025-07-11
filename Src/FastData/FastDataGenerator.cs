using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
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
    private const StringComparison DefaultStringComparison = StringComparison.Ordinal;

    /// <summary>Generate source code for the provided data.</summary>
    /// <param name="data">The data to generate from. Note that all objects must be the same type.</param>
    /// <param name="fdCfg">The configuration to use.</param>
    /// <param name="generator">The code generator to use.</param>
    /// <param name="factory">A logging factory</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when you gave an unsupported data type in data.</exception>
    public static string Generate(object[] data, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null) => data[0] switch
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
        string => GenerateString(Cast<string>(data), fdCfg, generator, factory),
        _ => throw new InvalidOperationException($"Unsupported data type: {data[0].GetType().Name}")
    };

    /// <summary>Generate source code for the provided data.</summary>
    /// <param name="data">The data to generate from.</param>
    /// <param name="fdCfg">The configuration to use.</param>
    /// <param name="generator">The code generator to use.</param>
    /// <param name="factory">A logging factory</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when you gave an unsupported data type in data.</exception>
    public static string Generate<T>(T[] data, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (data.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (data is string[] strArr)
            return GenerateString(strArr, fdCfg, generator, factory);

        Type type = typeof(T);

        if (type != typeof(char) && type != typeof(sbyte) && type != typeof(byte) && type != typeof(short) && type != typeof(ushort) && type != typeof(int) && type != typeof(uint) && type != typeof(long) && type != typeof(ulong) && type != typeof(float) && type != typeof(double))
            throw new InvalidOperationException($"Unsupported data type: {type.Name}");

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

        DataType dataType = (DataType)Enum.Parse(typeof(DataType), type.Name);
        LogDataType(logger, dataType);

        HashDetails hashDetails = new HashDetails();

        ValueProperties<T> valProps = DataAnalyzer.GetValueProperties(data);
        LogMinMaxValues(logger, valProps.MinValue, valProps.MaxValue);
        GeneratorConfig<T> genCfg = new GeneratorConfig<T>(fdCfg.StructureType, dataType, (uint)data.Length, valProps, hashDetails, GeneratorFlags.None);

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (data.Length == 1)
                    return Generate(generator, genCfg, new SingleValueStructure<T>(), data);

                // For small amounts of data, logic is the fastest. However, it increases the assembly size, so we want to try some special cases first.
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
                HashFunc<T> hashFunc = PrimitiveHash.GetHash<T>(dataType, valProps.HasZeroOrNaN);
                hashDetails.HasZeroOrNaN = valProps.HasZeroOrNaN;

                HashData hashData = HashData.Create(data, fdCfg.HashCapacityFactor, hashFunc);

                if (hashData.HashCodesPerfect)
                    return Generate(generator, genCfg, new HashSetPerfectStructure<T>(hashData, dataType), data);

                return Generate(generator, genCfg, new HashSetChainStructure<T>(hashData, dataType), data);
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static string GenerateString(string[] data, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        factory ??= NullLoggerFactory.Instance;

        //Validate that we only have unique data
        HashSet<string> uniq = new HashSet<string>(StringComparer.Ordinal);

        foreach (string val in data)
        {
            if (!uniq.Add(val))
                throw new InvalidOperationException($"Duplicate data found: {val}");
        }

        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));
        LogUserStructureType(logger, fdCfg.StructureType);
        LogUniqueItems(logger, uniq.Count);

        const DataType dataType = DataType.String;
        LogDataType(logger, dataType);

        StringProperties strProps = DataAnalyzer.GetStringProperties(data);
        LogMinMaxLength(logger, strProps.LengthData.Min, strProps.LengthData.Max);

        HashDetails hashDetails = new HashDetails();
        GeneratorConfig<string> genCfg = new GeneratorConfig<string>(fdCfg.StructureType, dataType, (uint)data.Length, strProps, DefaultStringComparison, hashDetails, generator.Encoding, strProps.CharacterData.AllAscii ? GeneratorFlags.AllAreASCII : GeneratorFlags.None);

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (data.Length == 1)
                    return Generate(generator, genCfg, new SingleValueStructure<string>(), data);

                // For small amounts of data, logic is the fastest. However, it increases the assembly size, so we want to try some special cases first.

                // If strings have unique lengths, we prefer to use a KeyLengthStructure.
                if (strProps.LengthData.Unique)
                    return Generate(generator, genCfg, new KeyLengthStructure<string>(strProps), data);

                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (data.Length < 400)
                    return Generate(generator, genCfg, new ConditionalStructure<string>(), data);

                goto case StructureType.HashSet;
            }
            case StructureType.Array:
                return Generate(generator, genCfg, new ArrayStructure<string>(), data);
            case StructureType.Conditional:
                return Generate(generator, genCfg, new ConditionalStructure<string>(), data);
            case StructureType.BinarySearch:
                return Generate(generator, genCfg, new BinarySearchStructure<string>(dataType, DefaultStringComparison), data);
            case StructureType.HashSet:
            {
                StringHashFunc hashFunc;

                if (fdCfg.StringAnalyzerConfig != null)
                {
                    Candidate candidate = GetBestHash(data, strProps, fdCfg.StringAnalyzerConfig, factory, generator.Encoding, true);
                    LogStringHashFitness(logger, candidate.Fitness);

                    Expression<StringHashFunc> expression = candidate.StringHash.GetExpression();
                    hashDetails.StringHash = new StringHashDetails(expression, candidate.StringHash.Functions, candidate.StringHash.State);

                    hashFunc = expression.Compile();
                }
                else
                {
                    hashFunc = DefaultStringHash.GetInstance(generator.Encoding).GetExpression().Compile();

                    //We do not set hashDetails.StringHash here, as the user requested it to be disabled
                }

                HashData hashData = HashData.Create(data, fdCfg.HashCapacityFactor, x =>
                {
                    byte[] bytes = generator.Encoding == GeneratorEncoding.UTF8 ? Encoding.UTF8.GetBytes(x) : Encoding.Unicode.GetBytes(x);
                    return hashFunc(bytes, bytes.Length);
                });

                if (hashData.HashCodesPerfect)
                    return Generate(generator, genCfg, new HashSetPerfectStructure<string>(hashData, dataType), data);

                return Generate(generator, genCfg, new HashSetChainStructure<string>(hashData, dataType), data);
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static string Generate<T, TContext>(ICodeGenerator generator, GeneratorConfig<T> genCfg, IStructure<T, TContext> structure, T[] data) where TContext : IContext<T>
    {
        TContext res = structure.Create(data);
        return generator.Generate(genCfg, res);
    }

    private static T[] Cast<T>(this object[] data)
    {
        T[] newArr = new T[data.Length];

        for (int i = 0; i < data.Length; i++)
            newArr[i] = (T)data[i];

        return newArr;
    }

    internal static Candidate GetBestHash(ReadOnlySpan<string> data, StringProperties props, StringAnalyzerConfig cfg, ILoggerFactory factory, GeneratorEncoding encoding, bool includeDefault)
    {
        Simulator sim = new Simulator(data.Length, encoding);

        //Run each of the analyzers
        List<Candidate> candidates = new List<Candidate>();

        //We always add the default hash as a candidate
        if (includeDefault)
            candidates.Add(sim.Run(data, DefaultStringHash.GetInstance(encoding)));

        if (cfg.BruteForceAnalyzerConfig != null)
        {
            BruteForceAnalyzer bf = new BruteForceAnalyzer(props, cfg.BruteForceAnalyzerConfig, sim, factory.CreateLogger<BruteForceAnalyzer>());
            if (bf.IsAppropriate())
                candidates.AddRange(bf.GetCandidates(data));
        }

        if (cfg.GeneticAnalyzerConfig != null)
        {
            GeneticAnalyzer ga = new GeneticAnalyzer(props, cfg.GeneticAnalyzerConfig, sim, factory.CreateLogger<GeneticAnalyzer>());
            if (ga.IsAppropriate())
                candidates.AddRange(ga.GetCandidates(data));
        }

        if (cfg.GPerfAnalyzerConfig != null)
        {
            GPerfAnalyzer ha = new GPerfAnalyzer(data.Length, props, cfg.GPerfAnalyzerConfig, sim, factory.CreateLogger<GPerfAnalyzer>());
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
        byte[] testBytes = encoding == GeneratorEncoding.UTF8 ? Encoding.UTF8.GetBytes(test) : Encoding.Unicode.GetBytes(test);

        //We start with the perfect results (if any)
        if (perfect.Count > 0)
        {
            foreach (Candidate candidate in perfect)
                Benchmark(testBytes, cfg.BenchmarkIterations, candidate);

            //Sort by time
            perfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));

            //Take the first non-perfect candidate (the highest fitness) and benchmark it too
            if (notPerfect.Count > 0)
            {
                Candidate np = notPerfect[0];
                Benchmark(testBytes, cfg.BenchmarkIterations, np);

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
            Benchmark(testBytes, cfg.BenchmarkIterations, candidate);

        notPerfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return notPerfect[0];
    }

    private static void Benchmark(byte[] data, int iterations, Candidate candidate)
    {
        //The candidate has already been benchmarked. Do nothing.
        if (candidate.Time >= double.Epsilon)
            return;

        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        //Warmup
        for (int i = 0; i < iterations; i++)
            func(data, data.Length);

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
            func(data, data.Length);

        sw.Stop();

        candidate.Time = sw.ElapsedTicks / (double)iterations;
    }
}