using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData;

[SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters")]
public static partial class FastDataGenerator
{
    public static string Generate<TKey>(ReadOnlyMemory<TKey> keys, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal(keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    // We need this overload as otherwise array expressions don't work for the user
    public static string Generate<TKey>(TKey[] keys, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    public static string GenerateKeyed<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null) where TKey : struct
    {
        return GenerateNumericInternal(keys, values, fdCfg, generator, factory);
    }

    // We need this overload as otherwise array expressions don't work for the user
    public static string GenerateKeyed<TKey, TValue>(TKey[] keys, TValue[] values, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, (ReadOnlyMemory<TValue>)values, fdCfg, generator, factory);
    }

    public static string Generate(ReadOnlyMemory<string> keys, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    // We need this overload as otherwise array expressions don't work for the user
    public static string Generate(string[] keys, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(new ReadOnlyMemory<string>(keys), ReadOnlyMemory<byte>.Empty, fdCfg, generator, factory);
    }

    public static string GenerateKeyed<TValue>(ReadOnlyMemory<string> keys, ReadOnlyMemory<TValue> values, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, values, fdCfg, generator, factory);
    }

    // We need this overload as otherwise array expressions don't work for the user
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
        bool sorted = Deduplication.DeduplicateKeys(cfg, ref keys, ref values, comparer, comparer);
        int newCount = keys.Length;

        if (oldCount == newCount)
            LogNumberOfKeys(logger, oldCount);
        else
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        LogKeyType(logger, nameof(String));

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(keys.Span, cfg.EnablePrefixSuffixTrimming, cfg.IgnoreCase, generator.Encoding);

        if (!props.CharacterData.AllAscii && generator.Encoding == GeneratorEncoding.ASCII)
            throw new InvalidOperationException("Your data has non-ASCII in it, and the generator is set to produce an ASCII API. That's not supported.");

        StringHashInfo? cacheHashInfo = null;
        HashData? cacheHashData = null;

        Type structureType;

        if (cfg.StructureTypeOverride != null)
            structureType = cfg.StructureTypeOverride;
        else
            structureType = StringStructures.GetBest(keys, !values.IsEmpty, props.LengthData.LengthMap.Min, props.LengthData.LengthMap.Max, cfg.AllowApproximateMatching, props.LengthData.UniqueLengths, cfg.StructureConfig, x => EnsureHashData(x.Span));

        LogStructureType(logger, structureType.Name);

        string trimPrefix = string.Empty;
        string trimSuffix = string.Empty;

        // If we can remove prefix/suffix from the keys, we do so.
        if (props.DeltaData.Prefix.Length > 0 || props.DeltaData.Suffix.Length > 0)
        {
            trimPrefix = props.DeltaData.Prefix;
            trimSuffix = props.DeltaData.Suffix;
            keys = StringTransform.SubStringKeys(keys.Span, props);
        }

        LogMinMaxLength(logger, props.LengthData.LengthMap.Min, props.LengthData.LengthMap.Max);

        IStructure<string, TValue, IContext> structure = StringStructureFactory<TValue>(structureType, props, () => EnsureHashData(keys.Span), comparer, sorted);

        // Combine mandatory early exits from structures with generated early exits from analysis
        IEarlyExit[] earlyExits = CombineEarlyExits(structure.GetMandatoryExits(), StringEarlyExits.GetCandidates(structureType, props, cfg.EarlyExitConfig, cfg.IgnoreCase));

        UsedFunctionVisitor usedVisitor = new UsedFunctionVisitor();
        AnnotatedExpr[] exprs = new AnnotatedExpr[earlyExits.Length];

        // Convert the early exits into a set of annotated expressions. We assume the input is called "key".
        ParameterExpression inputKey = Parameter(typeof(string), "key");

        for (int i = 0; i < earlyExits.Length; i++)
        {
            Expression expression = earlyExits[i].GetExpression(inputKey);
            usedVisitor.Visit(expression); // Update string functions with functions used in early exits
            exprs[i] = new AnnotatedExpr(expression, ExprKind.EarlyExit);
        }

        // Now we transform the expressions into more efficient representations
        AnnotatedExpr[] transformed = ExpressionHelper.Transform(exprs, [new AllocationGatherTransform()]).ToArray();

        // Walk the hash expression (if any) and update used functions
        if (cacheHashInfo != null)
            usedVisitor.Visit(cacheHashInfo.Expression);

        IContext res = structure.Create(keys, values);
        StringGeneratorConfig genCfg = new StringGeneratorConfig(structureType, (uint)keys.Length, props.LengthData.LengthMap.Min,
            props.LengthData.LengthMap.Max, cfg.IgnoreCase, props.CharacterData.CharacterClasses,
            generator.Encoding, transformed, trimPrefix, trimSuffix, cfg.TypeReductionEnabled, cacheHashInfo, usedVisitor.Functions);

        return generator.Generate<string, TValue>(genCfg, res);

        HashData EnsureHashData(ReadOnlySpan<string> keySpan)
        {
            if (cacheHashData != null)
                return cacheHashData;

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
                Candidate candidate = HashBenchmark.GetBestHash(keySpan, props, cfg.StringAnalyzerConfig, factory, generator.Encoding, true);
                LogStringHashFitness(logger, candidate.Fitness);

                Expression<StringHashFunc> expression = candidate.StringHash.GetExpression();
                info = new StringHashInfo(expression, candidate.StringHash.AdditionalData);
                hashFunc = expression.Compile();
            }
            else
            {
                Expression<StringHashFunc> expression = DefaultStringHash.GetInstance(generator.Encoding).GetExpression();
                info = new StringHashInfo(expression, null);
                hashFunc = expression.Compile();
            }

            HashData hashData = HashData.Create(keySpan, cfg.HashCapacityFactor, x =>
            {
                //TODO: Optimize this. Maybe reuse the same buffer. Benchmark it
                byte[] bytes = generator.Encoding == GeneratorEncoding.UTF8 ? Encoding.UTF8.GetBytes(x) : Encoding.Unicode.GetBytes(x);
                return hashFunc(bytes, bytes.Length);
            });

            return (hashData, info);
        }
    }

    private static IStructure<string, TValue, IContext> StringStructureFactory<TValue>(Type type, StringKeyProperties props, Func<HashData> getHashData, StringComparer comparer, bool sorted)
    {
        if (type == typeof(ArrayStructure<,>))
            return new ArrayStructure<string, TValue>();
        if (type == typeof(BinarySearchStructure<,>))
            return new BinarySearchStructure<string, TValue>(sorted, comparer);
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
        if (type == typeof(KeyLengthStructure<,>))
            return new KeyLengthStructure<string, TValue>(props.LengthData.LengthMap.Min, props.LengthData.LengthMap.Max);
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

        factory ??= NullLoggerFactory.Instance;
        ILogger logger = factory.CreateLogger(typeof(FastDataGenerator));
        LogKeyType(logger, type.Name);

        int oldCount = keys.Length;
        bool sorted = Deduplication.DeduplicateKeys(cfg, ref keys, ref values, EqualityComparer<TKey>.Default, Comparer<TKey>.Default);
        int newCount = keys.Length;

        if (oldCount == newCount) // No duplicates removed
            LogNumberOfKeys(logger, oldCount);
        else // Duplicates removed
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        //TODO: Only produce hash data when structure is indexed
        NumericKeyProperties<TKey> props = KeyAnalyzer.GetNumericProperties(keys, sorted);
        LogMinMaxValues(logger, props.MinKeyValue, props.MaxKeyValue);

        HashData? cacheHashData = null;

        Type structureType;

        if (cfg.StructureTypeOverride != null)
        {
            structureType = cfg.StructureTypeOverride;

            if (structureType == typeof(HashTableStructure<,>) || structureType == typeof(HashTableCompactStructure<,>) || structureType == typeof(HashTablePerfectStructure<,>) || structureType == typeof(BloomFilterStructure<,>))
                cacheHashData = GetNumericHash(keys.Span);
        }
        else
        {
            structureType = NumericStructures<TKey>.GetBest(keys, !values.IsEmpty, props.Density, props.IsConsecutive, cfg.AllowApproximateMatching, cfg.StructureConfig, x =>
            {
                return cacheHashData = GetNumericHash(x.Span);
            });
        }

        LogStructureType(logger, structureType.Name);

        IStructure<TKey, TValue, IContext> structure = NumericStructureFactory<TKey, TValue>(cfg, structureType, props, cacheHashData!, sorted);

        IEarlyExit[] earlyExits = CombineEarlyExits(structure.GetMandatoryExits(), NumericEarlyExits<TKey>.GetCandidates(type, props.MinKeyValue, props.MaxKeyValue, props.Range, props.BitMask, (uint)keys.Length, cfg.EarlyExitConfig));

        AnnotatedExpr[] exprs = new AnnotatedExpr[earlyExits.Length];

        // Convert the early exits into a set of annotated expressions. We assume the input is called "key".
        ParameterExpression inputKey = Parameter(typeof(TKey), "key");

        for (int i = 0; i < earlyExits.Length; i++)
            exprs[i] = new AnnotatedExpr(earlyExits[i].GetExpression(inputKey), ExprKind.EarlyExit);

        IContext res = structure.Create(keys, values);
        NumericGeneratorConfig genCfg = new NumericGeneratorConfig(structureType, (uint)keys.Length, props.MinKeyValue, props.MaxKeyValue, exprs, cfg.TypeReductionEnabled, props.HasZeroOrNaN);

        return generator.Generate<TKey, TValue>(genCfg, res);

        HashData GetNumericHash(ReadOnlySpan<TKey> keySpan)
        {
            NumericHashFunc<TKey> hashFunc = DefaultNumericHash.GetHashFunc<TKey>(props.HasZeroOrNaN);
            HashData hashData = HashData.Create(keySpan, cfg.HashCapacityFactor, x => hashFunc(x));
            return hashData;
        }
    }

    private static IEarlyExit[] CombineEarlyExits(IEnumerable<IEarlyExit> mandatoryExits, IEnumerable<IEarlyExit> candidateExits)
    {
        HashSet<IEarlyExit> combined = new HashSet<IEarlyExit>();

        foreach (IEarlyExit exit in mandatoryExits)
            combined.Add(exit);

        foreach (IEarlyExit exit in candidateExits)
            combined.Add(exit);

        return combined.ToArray();
    }

    private static IStructure<TKey, TValue, IContext> NumericStructureFactory<TKey, TValue>(DataConfig cfg, Type type, NumericKeyProperties<TKey> props, HashData hashData, bool sorted)
    {
        if (type == typeof(ArrayStructure<,>))
            return new ArrayStructure<TKey, TValue>();
        if (type == typeof(BinarySearchStructure<,>))
            return new BinarySearchStructure<TKey, TValue>(sorted, null);
        if (type == typeof(BinarySearchInterpolationStructure<,>))
            return new BinarySearchInterpolationStructure<TKey, TValue>(sorted);
        if (type == typeof(BitSetStructure<,>))
            return new BitSetStructure<TKey, TValue>(props);
        if (type == typeof(BloomFilterStructure<,>))
            return new BloomFilterStructure<TKey, TValue>(hashData);
        if (type == typeof(ConditionalStructure<,>))
            return new ConditionalStructure<TKey, TValue>();
        if (type == typeof(EliasFanoStructure<,>))
            return new EliasFanoStructure<TKey, TValue>(props.MinKeyValue, props.MaxKeyValue, props.ValueConverter, sorted, GetSetting(cfg, "SkipQuantum", 128));
        if (type == typeof(HashTableStructure<,>))
            return new HashTableStructure<TKey, TValue>(hashData);
        if (type == typeof(HashTableCompactStructure<,>))
            return new HashTableCompactStructure<TKey, TValue>(hashData);
        if (type == typeof(HashTablePerfectStructure<,>))
            return new HashTablePerfectStructure<TKey, TValue>(hashData);
        if (type == typeof(RangeStructure<,>))
            return new RangeStructure<TKey, TValue>(props.MinKeyValue, props.MaxKeyValue);
        if (type == typeof(RrrBitVectorStructure<,>))
            return new RrrBitVectorStructure<TKey, TValue>(props.MinKeyValue, props.MaxKeyValue, sorted);
        if (type == typeof(SingleValueStructure<,>))
            return new SingleValueStructure<TKey, TValue>();

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
        internal StringFunction Functions { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!Enum.TryParse(node.Method.Name, false, out StringFunction value))
                throw new InvalidOperationException($"The method '{node.Method.Name}' is unknown.");

            Functions |= value;

            return base.VisitMethodCall(node);
        }
    }
}