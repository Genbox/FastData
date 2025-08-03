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

    public static string GenerateKeyed<TKey, TValue>(TKey[] keys, TValue[] values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys is string[] strArr)
            return GenerateInternalString(strArr, values, fdCfg, generator, factory);

        return GenerateInternal(keys, values, fdCfg, generator, factory);
    }

    /// <summary>Generate source code for the provided data.</summary>
    /// <param name="keys">The data to generate from.</param>
    /// <param name="fdCfg">The configuration to use.</param>
    /// <param name="generator">The code generator to use.</param>
    /// <param name="factory">A logging factory</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when you gave an unsupported data type in data.</exception>
    public static string Generate<TKey>(TKey[] keys, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys is string[] strArr)
            return GenerateInternalString<byte>(strArr, null, fdCfg, generator, factory);

        return GenerateInternal<TKey, byte>(keys, null, fdCfg, generator, factory);
    }

    private static string GenerateInternal<TKey, TValue>(TKey[] keys, TValue[]? values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.Length == 0 || values?.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (values != null && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        Type type = typeof(TKey);

        if (type != typeof(char) && type != typeof(sbyte) && type != typeof(byte) && type != typeof(short) && type != typeof(ushort) && type != typeof(int) && type != typeof(uint) && type != typeof(long) && type != typeof(ulong) && type != typeof(float) && type != typeof(double))
            throw new InvalidOperationException($"Unsupported data type: {type.Name}");

        factory ??= NullLoggerFactory.Instance;

        //Validate that we only have unique data
        HashSet<TKey> uniq = new HashSet<TKey>();

        foreach (TKey key in keys)
        {
            if (!uniq.Add(key))
                throw new InvalidOperationException($"Duplicate data found: {key}");
        }

        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));
        LogUserStructureType(logger, fdCfg.StructureType);
        LogUniqueItems(logger, uniq.Count);

        KeyType keyType = (KeyType)Enum.Parse(typeof(KeyType), type.Name);
        LogKeyType(logger, keyType);

        HashDetails hashDetails = new HashDetails();

        ValueProperties<TKey> valProps = KeyAnalyzer.GetValueProperties(keys);
        LogMinMaxValues(logger, valProps.MinKeyValue, valProps.MaxKeyValue);
        GeneratorConfig<TKey> genCfg = new GeneratorConfig<TKey>(fdCfg.StructureType, keyType, (uint)keys.Length, valProps, hashDetails, GeneratorFlags.None);

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (keys.Length == 1)
                    return GenerateWrapper(generator, genCfg, new SingleValueStructure<TKey, TValue>(), keys, values);

                // For small amounts of data, logic is the fastest. However, it increases the assembly size, so we want to try some special cases first.
                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (keys.Length < 400)
                    return GenerateWrapper(generator, genCfg, new ConditionalStructure<TKey, TValue>(), keys, values);

                goto case StructureType.HashTable;
            }
            case StructureType.Array:
                return GenerateWrapper(generator, genCfg, new ArrayStructure<TKey, TValue>(), keys, values);
            case StructureType.Conditional:
                return GenerateWrapper(generator, genCfg, new ConditionalStructure<TKey, TValue>(), keys, values);
            case StructureType.BinarySearch:
                return GenerateWrapper(generator, genCfg, new BinarySearchStructure<TKey, TValue>(keyType, DefaultStringComparison), keys, values);
            case StructureType.HashTable:
            {
                HashFunc<TKey> hashFunc = PrimitiveHash.GetHash<TKey>(keyType, valProps.HasZeroOrNaN);
                hashDetails.HasZeroOrNaN = valProps.HasZeroOrNaN;

                HashData hashData = HashData.Create(keys, fdCfg.HashCapacityFactor, hashFunc);

                if (hashData.HashCodesPerfect)
                    return GenerateWrapper(generator, genCfg, new HashTablePerfectStructure<TKey, TValue>(hashData, keyType), keys, values);

                return GenerateWrapper(generator, genCfg, new HashTableStructure<TKey, TValue>(hashData, keyType), keys, values);
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static string GenerateInternalString<TValue>(string[] keys, TValue[]? values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.Length == 0 || values?.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (values != null && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        factory ??= NullLoggerFactory.Instance;

        //Validate that we only have unique data
        HashSet<string> uniq = new HashSet<string>(StringComparer.Ordinal);

        foreach (string val in keys)
        {
            if (!uniq.Add(val))
                throw new InvalidOperationException($"Duplicate data found: {val}");
        }

        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));
        LogUserStructureType(logger, fdCfg.StructureType);
        LogUniqueItems(logger, uniq.Count);

        const KeyType keyType = KeyType.String;
        LogKeyType(logger, keyType);

        StringProperties strProps = KeyAnalyzer.GetStringProperties(keys);
        LogMinMaxLength(logger, strProps.LengthData.Min, strProps.LengthData.Max);

        HashDetails hashDetails = new HashDetails();
        GeneratorConfig<string> genCfg = new GeneratorConfig<string>(fdCfg.StructureType, keyType, (uint)keys.Length, strProps, DefaultStringComparison, hashDetails, generator.Encoding, strProps.CharacterData.AllAscii ? GeneratorFlags.AllAreASCII : GeneratorFlags.None);

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (keys.Length == 1)
                    return GenerateWrapper(generator, genCfg, new SingleValueStructure<string, TValue>(), keys, values);

                // For small amounts of data, logic is the fastest. However, it increases the assembly size, so we want to try some special cases first.
                double density = (double)keys.Length / (strProps.LengthData.Max - strProps.LengthData.Min + 1);

                // Use KeyLengthStructure only when string lengths are unique and density >= 75%
                if (strProps.LengthData.Unique  && density >= 0.75)
                    return GenerateWrapper(generator, genCfg, new KeyLengthStructure<string, TValue>(strProps), keys, values);

                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (keys.Length < 400)
                    return GenerateWrapper(generator, genCfg, new ConditionalStructure<string, TValue>(), keys, values);

                goto case StructureType.HashTable;
            }
            case StructureType.Array:
                return GenerateWrapper(generator, genCfg, new ArrayStructure<string, TValue>(), keys, values);
            case StructureType.Conditional:
                return GenerateWrapper(generator, genCfg, new ConditionalStructure<string, TValue>(), keys, values);
            case StructureType.BinarySearch:
                return GenerateWrapper(generator, genCfg, new BinarySearchStructure<string, TValue>(keyType, DefaultStringComparison), keys, values);
            case StructureType.HashTable:
            {
                StringHashFunc hashFunc;

                if (fdCfg.StringAnalyzerConfig != null)
                {
                    Candidate candidate = GetBestHash(keys, strProps, fdCfg.StringAnalyzerConfig, factory, generator.Encoding, true);
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

                HashData hashData = HashData.Create(keys, fdCfg.HashCapacityFactor, x =>
                {
                    byte[] bytes = generator.Encoding == GeneratorEncoding.UTF8 ? Encoding.UTF8.GetBytes(x) : Encoding.Unicode.GetBytes(x);
                    return hashFunc(bytes, bytes.Length);
                });

                if (hashData.HashCodesPerfect)
                    return GenerateWrapper(generator, genCfg, new HashTablePerfectStructure<string, TValue>(hashData, keyType), keys, values);

                return GenerateWrapper(generator, genCfg, new HashTableStructure<string, TValue>(hashData, keyType), keys, values);
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static string GenerateWrapper<TKey, TValue, TContext>(ICodeGenerator generator, GeneratorConfig<TKey> genCfg, IStructure<TKey, TValue, TContext> structure, TKey[] keys, TValue[]? values) where TContext : IContext<TValue>
    {
        TContext res = structure.Create(keys, values);
        return generator.Generate(genCfg, res);
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