using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.Helpers;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData;

/// <summary>Generates source code for static lookup data structures.</summary>
[SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters")]
public static partial class FastDataGenerator
{
    /// <summary>Generates source code for an exact membership lookup over numeric keys.</summary>
    /// <typeparam name="TKey">The numeric key type.</typeparam>
    /// <param name="keys">The keys to include in the generated lookup.</param>
    /// <param name="fdCfg">The numeric data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, contains unsupported values, or uses an unsupported key type.</exception>
    public static string Generate<TKey>(ReadOnlyMemory<TKey> keys, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal(keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact membership lookup over numeric keys.</summary>
    /// <typeparam name="TKey">The numeric key type.</typeparam>
    /// <param name="keys">The keys to include in the generated lookup.</param>
    /// <param name="fdCfg">The numeric data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, contains unsupported values, or uses an unsupported key type.</exception>
    public static string Generate<TKey>(TKey[] keys, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact key/value lookup over numeric keys.</summary>
    /// <typeparam name="TKey">The numeric key type.</typeparam>
    /// <typeparam name="TValue">The value type returned for matching keys.</typeparam>
    /// <param name="keys">The keys to include in the generated lookup.</param>
    /// <param name="values">The values associated with <paramref name="keys" />.</param>
    /// <param name="fdCfg">The numeric data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, key/value counts differ, contains unsupported values, or uses an unsupported key type.</exception>
    public static string GenerateKeyed<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null) where TKey : struct
    {
        return GenerateNumericInternal(keys, values, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact key/value lookup over numeric keys.</summary>
    /// <typeparam name="TKey">The numeric key type.</typeparam>
    /// <typeparam name="TValue">The value type returned for matching keys.</typeparam>
    /// <param name="keys">The keys to include in the generated lookup.</param>
    /// <param name="values">The values associated with <paramref name="keys" />.</param>
    /// <param name="fdCfg">The numeric data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, key/value counts differ, contains unsupported values, or uses an unsupported key type.</exception>
    public static string GenerateKeyed<TKey, TValue>(TKey[] keys, TValue[] values, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, (ReadOnlyMemory<TValue>)values, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact membership lookup over string keys.</summary>
    /// <param name="keys">The string keys to include in the generated lookup.</param>
    /// <param name="fdCfg">The string data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, contains null or empty strings, or is incompatible with the generator encoding.</exception>
    public static string Generate(ReadOnlyMemory<string> keys, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact membership lookup over string keys.</summary>
    /// <param name="keys">The string keys to include in the generated lookup.</param>
    /// <param name="fdCfg">The string data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, contains null or empty strings, or is incompatible with the generator encoding.</exception>
    public static string Generate(string[] keys, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(new ReadOnlyMemory<string>(keys), ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact key/value lookup over string keys.</summary>
    /// <typeparam name="TValue">The value type returned for matching keys.</typeparam>
    /// <param name="keys">The string keys to include in the generated lookup.</param>
    /// <param name="values">The values associated with <paramref name="keys" />.</param>
    /// <param name="fdCfg">The string data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, key/value counts differ, contains null or empty strings, or is incompatible with the generator encoding.</exception>
    public static string GenerateKeyed<TValue>(ReadOnlyMemory<string> keys, ReadOnlyMemory<TValue> values, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, values, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact key/value lookup over string keys.</summary>
    /// <typeparam name="TValue">The value type returned for matching keys.</typeparam>
    /// <param name="keys">The string keys to include in the generated lookup.</param>
    /// <param name="values">The values associated with <paramref name="keys" />.</param>
    /// <param name="fdCfg">The string data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, key/value counts differ, contains null or empty strings, or is incompatible with the generator encoding.</exception>
    public static string GenerateKeyed<TValue>(string[] keys, TValue[] values, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(new ReadOnlyMemory<string>(keys), (ReadOnlyMemory<TValue>)values, fdCfg, generator, factory);
    }

    private static string GenerateStringInternal<TValue>(ReadOnlyMemory<string> keys, ReadOnlyMemory<TValue> values, StringDataConfig cfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (!values.IsEmpty && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        factory ??= NullLoggerFactory.Instance;

        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));

        // We validate and copy data at the same time
        foreach (string? key in keys.Span) //TODO: Move together with deduplication.
        {
            if (key == null)
                throw new InvalidOperationException("Keys cannot contain null values.");

            if (key.Length == 0)
                throw new InvalidOperationException("Keys cannot contain empty strings.");
        }

        StringComparer comparer = cfg.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        int oldCount = keys.Length;
        Deduplication.DeduplicateKeys(cfg, ref keys, ref values, comparer, comparer);
        int newCount = keys.Length;

        if (oldCount == newCount)
            LogNumberOfKeys(logger, oldCount);
        else
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        LogKeyType(logger, nameof(String));

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(keys.Span, cfg.IgnoreCase, generator.Encoding);

        if (!props.CharacterData.AllAscii && generator.Encoding == GeneratorEncoding.AsciiBytes)
            throw new InvalidOperationException("Your data has non-ASCII in it, and the generator is set to produce an ASCII API. That's not supported.");

        LogMinMaxLength(logger, props.LengthData.MinCharLength, props.LengthData.MaxCharLength);

        StringHashInfo? cacheHashInfo = null;
        HashData? cacheHashData = null;
        IEnumerable<IEarlyExit> hashMandatoryExits = [];

        (Type structureType, IStructure<string, TValue, IContext> structure, IContext res) = cfg.StructureTypeOverride != null ? CreateSelectedStringStructure(cfg.StructureTypeOverride) : CreateBestStringStructure();
        LogStructureType(logger, structureType.Name);

        IEarlyExit[] analysisExits = StringEarlyExits.GetExits(structureType, props, cfg.EarlyExitConfig, cfg.IgnoreCase, (uint)keys.Length);
        List<IEarlyExit> mandatoryExits = new List<IEarlyExit>();

        // Ensure the structure's mandatory exits are added
        mandatoryExits.AddRange(hashMandatoryExits);
        mandatoryExits.AddRange(structure.GetMandatoryExits());

        List<IEarlyExit> earlyExits = CombineExits(mandatoryExits, analysisExits);

        if (cfg.EarlyExitConfig.OptimizeExpression)
            ReduceExits(earlyExits);

        UsedFunctionVisitor usedVisitor = new UsedFunctionVisitor();

        // All exits use "key" - length and char exits are adjusted to work on the original key
        ParameterExpression inputKey = Parameter(typeof(string), "key");
        AnnotatedExpr[] exprs = AnnotateExits(earlyExits, inputKey, usedVisitor);

        // Now we transform the expressions into more efficient representations
        AnnotatedExpr[] transformed = ExpressionHelper.Transform(exprs, [new AllocationGatherTransform()]).ToArray();

        // Walk the hash expression (if any) and update used functions
        if (cacheHashInfo != null)
            usedVisitor.Visit(cacheHashInfo.Expression);

        StringGeneratorConfig genCfg = new StringGeneratorConfig(structureType, (uint)keys.Length, props.LengthData.LengthRanges.Min, props.LengthData.LengthRanges.Max, cfg.IgnoreCase, props.CharacterData.CharacterClasses, generator.Encoding, transformed, cfg.TypeReductionEnabled, cacheHashInfo, usedVisitor.Functions);

        return generator.Generate<string, TValue>(genCfg, res);

        (Type StructureType, IStructure<string, TValue, IContext> Structure, IContext Context) CreateSelectedStringStructure(Type selectedType)
        {
            IStructure<string, TValue, IContext> selectedStructure = StringStructureFactory<TValue>(selectedType, props, () => EnsureHashData(keys.Span), generator.Encoding);
            IContext? selectedContext = selectedStructure.Create(keys, values);

            if (selectedContext == null)
                throw new InvalidOperationException($"The selected structure {selectedType.Name} failed to create.");

            return (selectedType, selectedStructure, selectedContext);
        }

        (Type StructureType, IStructure<string, TValue, IContext> Structure, IContext Context) CreateBestStringStructure()
        {
            StructureConfig structureConfig = cfg.StructureConfig.Clone();

            while (true)
            {
                Type selectedType = StringStructures.GetBest(keys, !values.IsEmpty, props.LengthData.LengthRanges.Min, props.LengthData.LengthRanges.Max, cfg.AllowApproximateMatching, props.LengthData.UniqueLengths, structureConfig, x => EnsureHashData(x.Span));
                IStructure<string, TValue, IContext> selectedStructure = StringStructureFactory<TValue>(selectedType, props, () => EnsureHashData(keys.Span), generator.Encoding);
                IContext? selectedContext = selectedStructure.Create(keys, values);

                if (selectedContext != null)
                    return (selectedType, selectedStructure, selectedContext);

                structureConfig.Disable(selectedType);
            }
        }

        HashData EnsureHashData(ReadOnlySpan<string> keySpan)
        {
            if (cacheHashData != null)
                return cacheHashData;

            // Hash analysis can be expensive, so structure selection and structure creation share the same result.
            (HashData hashData, StringHashInfo _) = GetStringHash(keySpan);
            cacheHashData = hashData;

            // cacheHashInfo = ...; //TODO: Disabled temporarily until i can look at the compiler again
            return hashData;
        }

        (HashData, StringHashInfo) GetStringHash(ReadOnlySpan<string> keySpan)
        {
            StringHashFunc hashFunc;
            StringHashInfo info;

            if (cfg.StringAnalyzerConfig != null)
            {
                Candidate candidate = HashBenchmark.GetBestHash(keySpan, props, cfg.StringAnalyzerConfig, factory, generator.Encoding, true, cfg.IgnoreCase);
                LogStringHashFitness(logger, candidate.Fitness);

                Expression<StringHashFunc> expression = candidate.StringHash.GetExpression();
                info = new StringHashInfo(expression, candidate.StringHash.AdditionalData);
                hashFunc = expression.Compile();
                hashMandatoryExits = candidate.StringHash.GetMandatoryExits();
            }
            else
            {
                DefaultStringHash stringHash = DefaultStringHash.GetInstance(generator.Encoding);
                Expression<StringHashFunc> expression = stringHash.GetExpression();
                info = new StringHashInfo(expression, null);
                hashFunc = expression.Compile();
                hashMandatoryExits = stringHash.GetMandatoryExits();
            }

            Func<string, byte[]> getBytes = StringHelper.GetBytesFunc(generator.Encoding);

            HashData hashData = HashData.Create(keySpan, cfg.HashCapacityFactor, x =>
            {
                //TODO: Optimize this. Maybe reuse the same buffer. Benchmark it
                byte[] bytes = getBytes(x);
                return hashFunc(bytes, bytes.Length);
            });

            return (hashData, info);
        }
    }

    private static IStructure<string, TValue, IContext> StringStructureFactory<TValue>(Type type, StringKeyProperties props, Func<HashData> getHashData, GeneratorEncoding encoding)
    {
        if (type == typeof(ArrayStructure<,>))
            return new ArrayStructure<string, TValue>();
        if (type == typeof(BinarySearchStructure<,>))
            return new BinarySearchStructure<string, TValue>();
        if (type == typeof(BloomFilterStructure<,>))
            return new BloomFilterStructure<string, TValue>(getHashData());
        if (type == typeof(ConditionalStructure<,>))
            return new ConditionalStructure<string, TValue>();
        if (type == typeof(HashTableStructure<,>))
            return new HashTableStructure<string, TValue>(getHashData());
        if (type == typeof(HashTableCompactStructure<,>))
            return new HashTableCompactStructure<string, TValue>(getHashData());
        if (type == typeof(HashTablePerfectStructure<,>))
            return new HashTablePerfectStructure<string, TValue>(getHashData());
        if (type == typeof(HybleStructure<,>))
            return new HybleStructure<string, TValue>(getHashData());
        if (type == typeof(KeyLengthStructure<,>))
            return new KeyLengthStructure<string, TValue>(props.LengthData.LengthRanges.Min, props.LengthData.LengthRanges.Max, encoding);
        if (type == typeof(SingleValueStructure<,>))
            return new SingleValueStructure<string, TValue>();

        throw new InvalidOperationException($"Unsupported DataStructure {type.Name}");
    }

    private static string GenerateNumericInternal<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, NumericDataConfig cfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.IsEmpty)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (!values.IsEmpty && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        Type type = typeof(TKey);

        if (type != typeof(char) && type != typeof(sbyte) && type != typeof(byte) && type != typeof(short) && type != typeof(ushort) && type != typeof(int) && type != typeof(uint) && type != typeof(long) && type != typeof(ulong) && type != typeof(float) && type != typeof(double))
            throw new InvalidOperationException($"Unsupported data type: {type.Name}");

        if (keys is ReadOnlyMemory<float> floats)
        {
            foreach (float key in floats.Span)
            {
                if (float.IsNaN(key) || float.IsInfinity(key))
                    throw new InvalidOperationException("Keys cannot contain NaN or Infinity values.");
            }
        }
        else if (keys is ReadOnlyMemory<double> doubles)
        {
            foreach (double key in doubles.Span)
            {
                if (double.IsNaN(key) || double.IsInfinity(key))
                    throw new InvalidOperationException("Keys cannot contain NaN or Infinity values.");
            }
        }

        factory ??= NullLoggerFactory.Instance;
        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));
        LogKeyType(logger, type.Name);

        int oldCount = keys.Length;
        Deduplication.DeduplicateKeys(cfg, ref keys, ref values, EqualityComparer<TKey>.Default, Comparer<TKey>.Default);
        int newCount = keys.Length;

        if (oldCount == newCount) // No duplicates removed
            LogNumberOfKeys(logger, oldCount);
        else // Duplicates removed
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        //TODO: Only produce hash data when structure is indexed
        NumericKeyProperties<TKey> props = KeyAnalyzer.GetNumericProperties(keys);
        LogMinMaxValues(logger, props.DataRanges.Min, props.DataRanges.Max);

        HashData? cacheHashData = null;

        (Type structureType, IStructure<TKey, TValue, IContext> structure, IContext res) = cfg.StructureTypeOverride != null ? CreateSelectedNumericStructure(cfg.StructureTypeOverride) : CreateBestNumericStructure();
        LogStructureType(logger, structureType.Name);

        // Early exits are generated from numeric properties and then merged with checks required by the structure itself.
        IEarlyExit[] exitsAnalyzed = NumericEarlyExits<TKey>.GetExits(structureType, props.DataRanges, props.Range, props.BitMask, (uint)keys.Length, cfg.EarlyExitConfig);
        List<IEarlyExit> exits = CombineExits(structure.GetMandatoryExits(), exitsAnalyzed);
        if (cfg.EarlyExitConfig.OptimizeExpression)
            ReduceExits(exits);

        // Convert the early exits into a set of annotated expressions. We assume the input is called "key".
        ParameterExpression inputKey = Parameter(typeof(TKey), "key");
        AnnotatedExpr[] exprs = AnnotateExits(exits, inputKey, null);

        NumericGeneratorConfig genCfg = new NumericGeneratorConfig(structureType, (uint)keys.Length, props.DataRanges.Min, props.DataRanges.Max, exprs, cfg.TypeReductionEnabled, props.HasZero);

        return generator.Generate<TKey, TValue>(genCfg, res);

        (Type StructureType, IStructure<TKey, TValue, IContext> Structure, IContext Context) CreateSelectedNumericStructure(Type selectedType)
        {
            IStructure<TKey, TValue, IContext> selectedStructure = NumericStructureFactory<TKey, TValue>(cfg, selectedType, props, () => EnsureNumericHash(keys.Span));
            IContext? selectedContext = selectedStructure.Create(keys, values);

            if (selectedContext == null)
                throw new InvalidOperationException($"The selected structure {selectedType.Name} failed to create.");

            return (selectedType, selectedStructure, selectedContext);
        }

        (Type StructureType, IStructure<TKey, TValue, IContext> Structure, IContext Context) CreateBestNumericStructure()
        {
            StructureConfig structureConfig = cfg.StructureConfig.Clone();

            while (true)
            {
                Type selectedType = NumericStructures<TKey>.GetBest(keys, !values.IsEmpty, props.Density, cfg.AllowApproximateMatching, props.DataRanges.Ranges.Count, structureConfig, x =>
                {
                    return EnsureNumericHash(x.Span);
                });
                IStructure<TKey, TValue, IContext> selectedStructure = NumericStructureFactory<TKey, TValue>(cfg, selectedType, props, () => EnsureNumericHash(keys.Span));
                IContext? selectedContext = selectedStructure.Create(keys, values);

                if (selectedContext != null)
                    return (selectedType, selectedStructure, selectedContext);

                structureConfig.Disable(selectedType);
            }
        }

        HashData EnsureNumericHash(ReadOnlySpan<TKey> keySpan)
        {
            if (cacheHashData != null)
                return cacheHashData;

            return cacheHashData = GetNumericHash(keySpan);
        }

        HashData GetNumericHash(ReadOnlySpan<TKey> keySpan)
        {
            NumericHashFunc<TKey> hashFunc = DefaultNumericHash.GetHashFunc<TKey>(props.HasZero);
            HashData hashData = HashData.Create(keySpan, cfg.HashCapacityFactor, hashFunc);
            return hashData;
        }
    }

    private static List<IEarlyExit> CombineExits(IEnumerable<IEarlyExit> mandatory, IEnumerable<IEarlyExit> candidates)
    {
        List<IEarlyExit> exits = new List<IEarlyExit>(8);

        foreach (IEarlyExit exit in mandatory)
            AddExit(exit);

        foreach (IEarlyExit exit in candidates)
            AddExit(exit);

        return exits;

        void AddExit(IEarlyExit exit)
        {
            for (int i = 0; i < exits.Count; i++)
            {
                if (EqualityComparer<IEarlyExit>.Default.Equals(exit, exits[i]))
                    return;
            }

            exits.Add(exit);
        }
    }

    private static void ReduceExits(List<IEarlyExit> exits)
    {
        // Some early exits are reducible. For example, a LengthLessThan with a value of 3 is worse than one with 4.
        // So if there are competing exits, we take the one with the best bounds.
        for (int i = exits.Count - 1; i >= 0; i--)
        {
            IEarlyExit current = exits[i];

            for (int j = exits.Count - 1; j >= 0; j--)
            {
                if (i == j)
                    continue;

                if (current.IsWorseThan(exits[j]))
                {
                    exits.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private static AnnotatedExpr[] AnnotateExits(List<IEarlyExit> exits, ParameterExpression inputKey, UsedFunctionVisitor? usedVisitor)
    {
        AnnotatedExpr[] exprs = new AnnotatedExpr[exits.Count];

        for (int i = 0; i < exits.Count; i++)
        {
            Expression expression = exits[i].GetExpression(inputKey);
            usedVisitor?.Visit(expression);
            exprs[i] = new AnnotatedExpr(expression, ExprKind.EarlyExit);
        }

        return exprs;
    }

    private static IStructure<TKey, TValue, IContext> NumericStructureFactory<TKey, TValue>(DataConfig cfg, Type type, NumericKeyProperties<TKey> props, Func<HashData> getHashData)
    {
        if (type == typeof(ArrayStructure<,>))
            return new ArrayStructure<TKey, TValue>();
        if (type == typeof(BinarySearchStructure<,>))
            return new BinarySearchStructure<TKey, TValue>();
        if (type == typeof(BinarySearchInterpolationStructure<,>))
            return new BinarySearchInterpolationStructure<TKey, TValue>();
        if (type == typeof(BitSetStructure<,>))
            return new BitSetStructure<TKey, TValue>(props);
        if (type == typeof(BloomFilterStructure<,>))
            return new BloomFilterStructure<TKey, TValue>(getHashData());
        if (type == typeof(ConditionalStructure<,>))
            return new ConditionalStructure<TKey, TValue>();
        if (type == typeof(EliasFanoStructure<,>))
            return new EliasFanoStructure<TKey, TValue>(props.DataRanges.Min, props.DataRanges.Max, GetSetting(cfg, "SkipQuantum", 128));
        if (type == typeof(HashTableStructure<,>))
            return new HashTableStructure<TKey, TValue>(getHashData());
        if (type == typeof(HashTableCompactStructure<,>))
            return new HashTableCompactStructure<TKey, TValue>(getHashData());
        if (type == typeof(HashTablePerfectStructure<,>))
            return new HashTablePerfectStructure<TKey, TValue>(getHashData());
        if (type == typeof(HybleStructure<,>))
            return new HybleStructure<TKey, TValue>(getHashData());
        if (type == typeof(RangeStructure<,>))
            return new RangeStructure<TKey, TValue>(props.DataRanges);
        if (type == typeof(RrrBitVectorStructure<,>))
            return new RrrBitVectorStructure<TKey, TValue>(props.DataRanges.Min, props.DataRanges.Max);
        if (type == typeof(SingleValueStructure<,>))
            return new SingleValueStructure<TKey, TValue>();
        if (type == typeof(PgmStructure<,>))
            return new PgmStructure<TKey, TValue>(GetSetting(cfg, "Epsilon", 64), GetSetting(cfg, "EpsilonRecursive", 4));

        throw new InvalidOperationException($"Unsupported DataStructure {type}");
    }

    private static T GetSetting<T>(DataConfig cfg, string key, T defaultValue)
    {
        if (!cfg.StructureSettings.TryGetValue(key, out object? value))
            return defaultValue;

        return (T)value;
    }

    private sealed class UsedFunctionVisitor : ExpressionVisitor
    {
        internal GeneratorFunction Functions { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!Enum.TryParse(node.Method.Name, false, out GeneratorFunction value))
                throw new InvalidOperationException($"The method '{node.Method.Name}' is unknown.");

            Functions |= value;

            return base.VisitMethodCall(node);
        }
    }
}