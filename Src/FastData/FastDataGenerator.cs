using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Extensions;
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
    public static string Generate<TKey>(ReadOnlyMemory<TKey> keys, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (typeof(TKey) == typeof(string))
            return GenerateStringInternal((ReadOnlyMemory<string>)(object)keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);

        return GenerateNumericInternal(keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    public static string Generate<TKey>(TKey[] keys, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (typeof(TKey) == typeof(string))
            return GenerateStringInternal(new ReadOnlyMemory<string>((string[])(object)keys), ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);

        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    public static string GenerateKeyed<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (typeof(TKey) == typeof(string))
            return GenerateStringInternal((ReadOnlyMemory<string>)(object)keys, values, fdCfg, generator, factory);

        return GenerateNumericInternal(keys, values, fdCfg, generator, factory);
    }

    public static string GenerateKeyed<TKey, TValue>(TKey[] keys, TValue[] values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (typeof(TKey) == typeof(string))
            return GenerateStringInternal(new ReadOnlyMemory<string>((string[])(object)keys), new ReadOnlyMemory<TValue>(values), fdCfg, generator, factory);

        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, (ReadOnlyMemory<TValue>)values, fdCfg, generator, factory);
    }

    public static string Generate(ReadOnlyMemory<string> keys, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    public static string Generate(string[] keys, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(new ReadOnlyMemory<string>(keys), ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    public static string GenerateKeyed<TValue>(ReadOnlyMemory<string> keys, ReadOnlyMemory<TValue> values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, values, fdCfg, generator, factory);
    }

    private static string GenerateStringInternal<TValue>(ReadOnlyMemory<string> keys, ReadOnlyMemory<TValue> values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (!values.IsEmpty && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        factory ??= NullLoggerFactory.Instance;

        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));
        LogUserStructureType(logger, fdCfg.StructureType);

        // We validate and copy data at the same time
        foreach (string? key in keys.Span)
        {
            if (key == null)
                throw new InvalidOperationException("Keys cannot contain null values.");

            if (key.Length == 0)
                throw new InvalidOperationException("Keys cannot contain empty strings.");
        }

        StringComparer comparer = fdCfg.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        int oldCount = keys.Length;
        bool sorted = DeduplicateKeys(fdCfg, ref keys, ref values, comparer, comparer);
        int newCount = keys.Length;

        if (oldCount == newCount)
            LogNumberOfKeys(logger, oldCount);
        else
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        LogKeyType(logger, nameof(String));

        StringKeyProperties strProps = KeyAnalyzer.GetStringProperties(keys.Span, fdCfg.EnablePrefixSuffixTrimming, fdCfg.IgnoreCase, generator.Encoding);

        string trimPrefix = string.Empty;
        string trimSuffix = string.Empty;

        // If we can remove prefix/suffix from the keys, we do so.
        if (strProps.DeltaData.Prefix.Length > 0 || strProps.DeltaData.Suffix.Length > 0)
        {
            trimPrefix = strProps.DeltaData.Prefix;
            trimSuffix = strProps.DeltaData.Suffix;
            keys = SubStringKeys(keys.Span, strProps);
        }

        LogMinMaxLength(logger, strProps.LengthData.LengthMap.Min, strProps.LengthData.LengthMap.Max);

        HashDetails hashDetails = new HashDetails();
        TempStringState<string, TValue> tempState = new TempStringState<string, TValue>(keys, values, fdCfg, generator, strProps, hashDetails, trimPrefix, trimSuffix);

        HashData GetStringHashData(ReadOnlySpan<string> keys2)
        {
            StringHashFunc hashFunc;

            if (fdCfg.StringAnalyzerConfig != null)
            {
                Candidate candidate = GetBestHash(keys2, strProps, fdCfg.StringAnalyzerConfig, factory, generator.Encoding, true, fdCfg);
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

            return HashData.Create(keys2, fdCfg.HashCapacityFactor, x =>
            {
                //TODO: Optimize this. Maybe reuse the same buffer. Benchmark it
                byte[] bytes = generator.Encoding == GeneratorEncoding.UTF8 ? Encoding.UTF8.GetBytes(x) : Encoding.Unicode.GetBytes(x);
                return hashFunc(bytes, bytes.Length);
            });
        }

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (fdCfg.AllowApproximateMatching && values.IsEmpty)
                {
                    HashData bloomHashData = GetStringHashData(keys.Span);
                    return GenerateWrapper(tempState, new BloomFilterStructure<string, TValue>(bloomHashData));
                }

                if (keys.Length == 1)
                    return GenerateWrapper(tempState, new SingleValueStructure<string, TValue>());

                // For small amounts of data, logic is the fastest. However, it increases the assembly size, so we want to try some special cases first.
                double density = (double)keys.Length / (strProps.LengthData.LengthMap.Max - strProps.LengthData.LengthMap.Min + 1);

                // Use KeyLengthStructure only when string lengths are unique and density >= threshold
                if (strProps.LengthData.Unique && density >= fdCfg.KeyLengthStructureMinDensity)
                    return GenerateWrapper(tempState, new KeyLengthStructure<string, TValue>(strProps));

                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (keys.Length < fdCfg.ConditionalStructureMaxItemCount)
                    return GenerateWrapper(tempState, new ConditionalStructure<string, TValue>());

                goto case StructureType.HashTable;
            }
            case StructureType.Array:
                return GenerateWrapper(tempState, new ArrayStructure<string, TValue>());
            case StructureType.Conditional:
                return GenerateWrapper(tempState, new ConditionalStructure<string, TValue>());
            case StructureType.BinarySearch:
                return GenerateWrapper(tempState, new BinarySearchStructure<string, TValue>(fdCfg.IgnoreCase, sorted));
            case StructureType.HashTable:
            {
                HashData hashData = GetStringHashData(keys.Span);

                if (hashData.HashCodesPerfect)
                    return GenerateWrapper(tempState, new HashTablePerfectStructure<string, TValue>(hashData));

                return GenerateWrapper(tempState, new HashTableStructure<string, TValue>(hashData));
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static string GenerateNumericInternal<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, FastDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.IsEmpty)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (!values.IsEmpty && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        Type type = typeof(TKey);

        if (type != typeof(char) && type != typeof(sbyte) && type != typeof(byte) && type != typeof(short) && type != typeof(ushort) && type != typeof(int) && type != typeof(uint) && type != typeof(long) && type != typeof(ulong) && type != typeof(float) && type != typeof(double))
            throw new InvalidOperationException($"Unsupported data type: {type.Name}");

        factory ??= NullLoggerFactory.Instance;
        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));

        int oldCount = keys.Length;
        bool sorted = DeduplicateKeys(fdCfg, ref keys, ref values, EqualityComparer<TKey>.Default, Comparer<TKey>.Default);
        int newCount = keys.Length;

        if (oldCount == newCount) // No duplicates removed
            LogNumberOfKeys(logger, oldCount);
        else // Duplicates removed
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        LogUserStructureType(logger, fdCfg.StructureType);

        LogKeyType(logger, typeof(TKey).Name);

        NumericKeyProperties<TKey> props = KeyAnalyzer.GetNumericProperties(keys, sorted);
        LogMinMaxValues(logger, props.MinKeyValue, props.MaxKeyValue);

        HashDetails hashDetails = new HashDetails();
        hashDetails.HasZeroOrNaN = props.HasZeroOrNaN;

        TempNumericState<TKey, TValue> tempState = new TempNumericState<TKey, TValue>(keys, values, fdCfg, generator, props, hashDetails);

        switch (fdCfg.StructureType)
        {
            case StructureType.Auto:
            {
                if (keys.Length == 1)
                    return GenerateWrapper(tempState, new SingleValueStructure<TKey, TValue>());

                // RangeStructure handles consecutive keys, but does not support values
                if (props.IsConsecutive && values.IsEmpty)
                    return GenerateWrapper(tempState, new RangeStructure<TKey, TValue>(props));

                if (fdCfg.AllowApproximateMatching)
                {
                    HashFunc<TKey> hashFunc = PrimitiveHash.GetHashFunc<TKey>(props.HasZeroOrNaN);
                    HashData bloomHashData = HashData.Create(keys.Span, fdCfg.HashCapacityFactor, hashFunc);
                    return GenerateWrapper(tempState, new BloomFilterStructure<TKey, TValue>(bloomHashData));
                }

                if (typeof(TKey) != typeof(float) && typeof(TKey) != typeof(double) && props.Range <= fdCfg.BitSetStructureMaxRange && props.Density >= fdCfg.BitSetStructureMinDensity)
                    return GenerateWrapper(tempState, new BitSetStructure<TKey, TValue>(props));

                // For small amounts of data, logic is the fastest. However, it increases the assembly size, so we want to try some special cases first.
                // Note: Experiments show it is at the ~500-element boundary that Conditional starts to become slower. Use 400 to be safe.
                if (keys.Length < fdCfg.ConditionalStructureMaxItemCount)
                    return GenerateWrapper(tempState, new ConditionalStructure<TKey, TValue>());

                if (values.IsEmpty && typeof(TKey).IsIntegral() && !props.IsConsecutive && keys.Length >= fdCfg.RrrBitVectorStructureMinItemCount && props.Density <= fdCfg.RrrBitVectorStructureMaxDensity)
                    return GenerateWrapper(tempState, new RrrBitVectorStructure<TKey, TValue>(sorted));

                if (values.IsEmpty && typeof(TKey).IsIntegral() && !props.IsConsecutive && keys.Length >= fdCfg.EliasFanoStructureMinItemCount && props.Density <= fdCfg.EliasFanoStructureMaxDensity)
                    return GenerateWrapper(tempState, new EliasFanoStructure<TKey, TValue>(props, fdCfg, sorted));

                goto case StructureType.HashTable;
            }
            case StructureType.Array:
                return GenerateWrapper(tempState, new ArrayStructure<TKey, TValue>());
            case StructureType.Conditional:
                return GenerateWrapper(tempState, new ConditionalStructure<TKey, TValue>());
            case StructureType.BinarySearch:
                if (IsWellDistributed(keys.Span, props, fdCfg.MaxHistogramBuckets))
                    return GenerateWrapper(tempState, new InterpolatedBinarySearchStructure<TKey, TValue>(sorted));

                return GenerateWrapper(tempState, new BinarySearchStructure<TKey, TValue>(fdCfg.IgnoreCase, sorted));
            case StructureType.HashTable:
            {
                HashFunc<TKey> hashFunc = PrimitiveHash.GetHashFunc<TKey>(props.HasZeroOrNaN);
                HashData hashData = HashData.Create(keys.Span, fdCfg.HashCapacityFactor, hashFunc);

                if (hashData.HashCodesPerfect)
                    return GenerateWrapper(tempState, new HashTablePerfectStructure<TKey, TValue>(hashData));

                return GenerateWrapper(tempState, new HashTableStructure<TKey, TValue>(hashData));
            }
            default:
                throw new InvalidOperationException($"Unsupported DataStructure {fdCfg.StructureType}");
        }
    }

    private static bool DeduplicateKeys<TKey, TValue>(FastDataConfig fdCfg, ref ReadOnlyMemory<TKey> keys, ref ReadOnlyMemory<TValue> values, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer)
    {
        // Apply the configured strategy and return new key/value buffers.
        if (fdCfg.DeduplicationMode == DeduplicationMode.Disabled)
        {
            return false;
        }

        TKey[] copyKeys = new TKey[keys.Length];
        keys.CopyTo(copyKeys);

        TValue[] copyValues = new TValue[values.Length];
        values.CopyTo(copyValues);

        bool isSorted = false;
        int uniqueCount;

        if (fdCfg.DeduplicationMode == DeduplicationMode.HashSetPreserveOrder)
            DeduplicateWithHashSet(copyKeys, copyValues, fdCfg.ThrowOnDuplicates, equalityComparer, out uniqueCount);
        else if (fdCfg.DeduplicationMode == DeduplicationMode.Sort)
        {
            DeduplicateWithSort(copyKeys, copyValues, fdCfg.ThrowOnDuplicates, equalityComparer, sortComparer, out uniqueCount);
            isSorted = true;
        }
        else if (fdCfg.DeduplicationMode == DeduplicationMode.SortPreserveOrder)
            DeduplicateWithSortPreserveInputOrder(copyKeys, copyValues, fdCfg.ThrowOnDuplicates, equalityComparer, sortComparer, out uniqueCount);
        else
            throw new InvalidOperationException("Unsupported deduplication mode: " + fdCfg.DeduplicationMode);

        keys = copyKeys.AsMemory(0, uniqueCount);
        values = copyValues.Length > 0 ? copyValues.AsMemory(0, uniqueCount) : copyValues;
        return isSorted;
    }

    internal static void DeduplicateWithHashSet<TKey, TValue>(TKey[] keys, TValue[] values, bool throwEnabled, IEqualityComparer<TKey> equalityComparer, out int uniqueCount)
    {
        HashSet<TKey> uniq = new HashSet<TKey>(equalityComparer);

        uniqueCount = 0;

        for (int i = 0; i < keys.Length; i++)
        {
            TKey key = keys[i];

            if (!uniq.Add(key))
            {
                if (throwEnabled)
                    throw new InvalidOperationException($"Duplicate key found: {key}");

                continue;
            }

            keys[uniqueCount] = key;

            if (values.Length != 0 && uniqueCount != i) // Check avoids swapping an element with itself
                values[uniqueCount] = values[i];

            uniqueCount++;
        }
    }

    internal static void DeduplicateWithSortPreserveInputOrder<TKey, TValue>(TKey[] keys, TValue[] values, bool throwEnabled, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer, out int uniqueCount)
    {
        // Create a map to keep track of the original order. We need it to map values back to the original order.
        // We also need it to map values (if any).
        int[] map = new int[keys.Length];

        for (int i = 0; i < map.Length; i++)
            map[i] = i;

        /*
             = Starting state =
             keys  values  map
              6     val6    1
              9     val9    2
              3     val3    3
              1     val1    4
              3     val3    5
        */

        Array.Sort(keys, map, sortComparer);

        /*
             = After sorting =
             keys  values  map
              1     val6    4
              3     val9    3
              3     val3    5
              6     val1    1
              9     val3    2
        */

        uniqueCount = 0;
        for (int read = 1; read < keys.Length; read++)
        {
            TKey key = keys[read];

            if (equalityComparer.Equals(key, keys[uniqueCount]))
            {
                if (throwEnabled)
                    throw new InvalidOperationException($"Duplicate key found: {key}");

                continue;
            }

            uniqueCount++;
            keys[uniqueCount] = key;
            map[uniqueCount] = map[read];
        }

        uniqueCount++; // It is off-by-one now. We correct it

        // Sort keys back to original order
        Array.Sort(map, keys, 0, uniqueCount);

        // If we have values, make sure they are compacted too
        if (values.Length != 0)
        {
            for (int i = 0; i < uniqueCount; i++)
                values[i] = values[map[i]];
        }
    }

    internal static void DeduplicateWithSort<TKey, TValue>(TKey[] keys, TValue[] values, bool throwEnabled, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer, out int uniqueCount)
    {
        if (values.Length > 0)
            Array.Sort(keys, values, sortComparer);
        else
            Array.Sort(keys, sortComparer);

        TKey current = keys[0];
        uniqueCount = 1;

        for (int i = 1; i < keys.Length; i++)
        {
            TKey key = keys[i];

            if (equalityComparer.Equals(key, current))
            {
                if (throwEnabled)
                    throw new InvalidOperationException($"Duplicate key found: {key}");

                continue;
            }

            keys[uniqueCount] = key;

            if (values.Length != 0 && uniqueCount != i) // Check avoids swapping an element with itself
                values[uniqueCount] = values[i];

            current = key;
            uniqueCount++;
        }
    }

    internal static string[] SubStringKeys(ReadOnlySpan<string> keys, StringKeyProperties props)
    {
        int prefix = props.DeltaData.Prefix.Length;
        int suffix = props.DeltaData.Suffix.Length;

        Debug.Assert(prefix > 0 || suffix > 0, "Don't call this method if there is nothing to trim");

        string[] modified = new string[keys.Length];

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            modified[i] = key.Substring(prefix, key.Length - prefix - suffix);
        }

        return modified;
    }

    private static string GenerateWrapper<TKey, TValue, TContext>(in TempStringState<TKey, TValue> state, IStructure<TKey, TValue, TContext> structure) where TContext : IContext
    {
        TContext res = structure.Create(state.Keys, state.Values);
        StringKeyProperties strProps = state.StringKeyProperties;
        GeneratorConfig<string> genCfg = new GeneratorConfig<string>(structure.GetType(), (uint)state.Keys.Length, strProps, state.HashDetails, state.Generator.Encoding, state.TrimPrefix, state.TrimSuffix, state.Config);
        return state.Generator.Generate<string, TValue>(genCfg, res);
    }

    private static string GenerateWrapper<TKey, TValue, TContext>(in TempNumericState<TKey, TValue> state, IStructure<TKey, TValue, TContext> structure) where TContext : IContext
    {
        TContext res = structure.Create(state.Keys, state.Values);
        GeneratorConfig<TKey> genCfg = new GeneratorConfig<TKey>(structure.GetType(), (uint)state.Keys.Length, state.NumericKeyProperties, state.HashDetails, state.Config);
        return state.Generator.Generate<TKey, TValue>(genCfg, res);
    }

    internal static Candidate GetBestHash(ReadOnlySpan<string> data, StringKeyProperties props, StringAnalyzerConfig cfg, ILoggerFactory factory, GeneratorEncoding encoding, bool includeDefault, FastDataConfig fdCfg)
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

        string test = new string('a', (int)props.LengthData.LengthMap.Max);
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

                double threshold = p.Time + (p.Time * fdCfg.PerfectHashMaxSlowdownFactor);

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

    private static bool IsWellDistributed<TKey>(ReadOnlySpan<TKey> keys, NumericKeyProperties<TKey> props, int maxHistogramBuckets)
    {
        if (keys.Length < 16)
            return false;

        int buckets = Math.Min(maxHistogramBuckets, keys.Length);
        if (buckets <= 1)
            return false;

        if (props.Range == 0)
            return false;

        Span<int> hist = stackalloc int[buckets];
        ulong min = (ulong)props.ValueConverter(props.MinKeyValue);

        if (typeof(TKey) == typeof(float))
        {
            for (int i = 0; i < keys.Length; i++)
            {
                float key = (float)(object)keys[i]!;
                if (float.IsNaN(key) || float.IsInfinity(key))
                    return false;

                ulong value = (ulong)(long)key;
                ulong diff = value - min;
                int bucketIndex = (int)((diff * (ulong)buckets) / props.Range);

                if ((uint)bucketIndex >= (uint)buckets)
                    bucketIndex = buckets - 1;

                hist[bucketIndex]++;
            }
        }
        else if (typeof(TKey) == typeof(double))
        {
            for (int i = 0; i < keys.Length; i++)
            {
                double key = (double)(object)keys[i]!;
                if (double.IsNaN(key) || double.IsInfinity(key))
                    return false;

                ulong value = (ulong)(long)key;
                ulong diff = value - min;
                int bucketIndex = (int)((diff * (ulong)buckets) / props.Range);

                if ((uint)bucketIndex >= (uint)buckets)
                    bucketIndex = buckets - 1;

                hist[bucketIndex]++;
            }
        }
        else
        {
            for (int i = 0; i < keys.Length; i++)
            {
                ulong value = (ulong)props.ValueConverter(keys[i]);
                ulong diff = value - min;
                int bucketIndex = (int)((diff * (ulong)buckets) / props.Range);

                if ((uint)bucketIndex >= (uint)buckets)
                    bucketIndex = buckets - 1;

                hist[bucketIndex]++;
            }
        }

        int minCount = int.MaxValue;
        int maxCount = 0;
        int sum = 0;

        for (int i = 0; i < buckets; i++)
        {
            int count = hist[i];
            sum += count;
            if (count < minCount)
                minCount = count;
            if (count > maxCount)
                maxCount = count;
        }

        if (minCount == 0)
            return false;

        int avg = sum / buckets;
        return maxCount - minCount <= avg;
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

    private readonly record struct TempStringState<TKey, TValue>(ReadOnlyMemory<TKey> Keys, ReadOnlyMemory<TValue> Values, FastDataConfig Config, ICodeGenerator Generator, StringKeyProperties StringKeyProperties, HashDetails HashDetails, string TrimPrefix, string TrimSuffix);
    private readonly record struct TempNumericState<TKey, TValue>(ReadOnlyMemory<TKey> Keys, ReadOnlyMemory<TValue> Values, FastDataConfig Config, ICodeGenerator Generator, NumericKeyProperties<TKey> NumericKeyProperties, HashDetails HashDetails);
}