using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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
    public static NumericGenerationResult Generate<TKey>(ReadOnlyMemory<TKey> keys, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal(keys, ReadOnlyMemory<byte>.Empty, false, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact membership lookup over numeric keys.</summary>
    /// <typeparam name="TKey">The numeric key type.</typeparam>
    /// <param name="keys">The keys to include in the generated lookup.</param>
    /// <param name="fdCfg">The numeric data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, contains unsupported values, or uses an unsupported key type.</exception>
    public static NumericGenerationResult Generate<TKey>(TKey[] keys, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, ReadOnlyMemory<byte>.Empty, false, fdCfg, generator, factory);
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
    public static NumericGenerationResult GenerateKeyed<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null) where TKey : struct
    {
        return GenerateNumericInternal(keys, values, true, fdCfg, generator, factory);
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
    public static NumericGenerationResult GenerateKeyed<TKey, TValue>(TKey[] keys, TValue[] values, NumericDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateNumericInternal((ReadOnlyMemory<TKey>)keys, (ReadOnlyMemory<TValue>)values, true, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact membership lookup over string keys.</summary>
    /// <param name="keys">The string keys to include in the generated lookup.</param>
    /// <param name="fdCfg">The string data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, contains null or empty strings, or is incompatible with the generator encoding.</exception>
    public static StringGenerationResult Generate(ReadOnlyMemory<string> keys, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, ReadOnlyMemory<byte>.Empty, false, fdCfg, generator, factory);
    }

    /// <summary>Generates source code for an exact membership lookup over string keys.</summary>
    /// <param name="keys">The string keys to include in the generated lookup.</param>
    /// <param name="fdCfg">The string data configuration.</param>
    /// <param name="generator">The target-language code generator.</param>
    /// <param name="factory">Optional logger factory used to report generation decisions.</param>
    /// <returns>The generated source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is empty, contains null or empty strings, or is incompatible with the generator encoding.</exception>
    public static StringGenerationResult Generate(string[] keys, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(new ReadOnlyMemory<string>(keys), ReadOnlyMemory<byte>.Empty, false, fdCfg, generator, factory);
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
    public static StringGenerationResult GenerateKeyed<TValue>(ReadOnlyMemory<string> keys, ReadOnlyMemory<TValue> values, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(keys, values, true, fdCfg, generator, factory);
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
    public static StringGenerationResult GenerateKeyed<TValue>(string[] keys, TValue[] values, StringDataConfig fdCfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        return GenerateStringInternal(new ReadOnlyMemory<string>(keys), (ReadOnlyMemory<TValue>)values, true, fdCfg, generator, factory);
    }

    private static StringGenerationResult GenerateStringInternal<TValue>(ReadOnlyMemory<string> keys, ReadOnlyMemory<TValue> values, bool hasValues, StringDataConfig cfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (hasValues && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        StructureCapability requiredCapabilities = GetRequiredCapabilities(cfg, hasValues);

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
        Deduplication.DeduplicateStringKeys(cfg, ref keys, ref values, comparer, comparer);
        int newCount = keys.Length;

        if (oldCount == newCount)
            LogNumberOfKeys(logger, oldCount);
        else
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        LogKeyType(logger, nameof(String));

        StringKeyProperties props = KeyAnalyzer.GetStringProperties(keys.Span, cfg.IgnoreCase, generator.Encoding);

        if (cfg.IgnoreCase && !props.CharacterData.AllAscii)
            throw new InvalidOperationException("IgnoreCase is only supported for ASCII string keys.");

        if (!props.CharacterData.AllAscii && generator.Encoding == GeneratorEncoding.AsciiBytes)
            throw new InvalidOperationException("Your data has non-ASCII in it, and the generator is set to produce an ASCII API. That's not supported.");

        LogMinMaxLength(logger, props.LengthData.MinByteLength, props.LengthData.MaxByteLength);

        StringHashInfo? cacheHashInfo = null;
        HashData? cacheHashData = null;
        IEnumerable<IEarlyExit> cacheHashExits = [];
        string? stringHashName = null;

        StructureType? structureTypeOverride = cfg.StructureTypeOverride is {} value && value != StructureType.Auto ? value : null;
        (StructureType structureType, IStructure<string, TValue, IContext> structure, IContext res) = structureTypeOverride.HasValue ? CreateSelectedStringStructure(structureTypeOverride.Value) : CreateBestStringStructure();
        LogStructureType(logger, structureType.ToString());

        IEarlyExit[] analysisExits = StringEarlyExits.GetExits(structureType, props, cfg.EarlyExitConfig, cfg.IgnoreCase, (uint)keys.Length);
        List<IEarlyExit> mandatoryExits = new List<IEarlyExit>();

        // Hash mandatory exits are populated by EnsureHashData during structure selection/creation.
        // They are read here after structure creation is complete, so the array is guaranteed to be populated if the selected structure is hash-based.
        mandatoryExits.AddRange(cacheHashExits); // From hash functions
        mandatoryExits.AddRange(structure.GetMandatoryExits()); // From the structure

        List<IEarlyExit> earlyExits = EarlyExitPipeline.CombineAndDedup(mandatoryExits, analysisExits);

        if (cfg.EarlyExitConfig.Optimize)
            EarlyExitPipeline.Optimize<string>(earlyExits);

        UsedFunctionVisitor usedVisitor = new UsedFunctionVisitor();

        ParameterExpression inputKey = Parameter(typeof(string), "key");

        // All exits use "key" - length and char exits are adjusted to work on the original key
        AnnotatedExpr[] exprs = EarlyExitPipeline.Annotate(earlyExits, inputKey, usedVisitor);

        // Only emit the length allocation when it is actually referenced: either by an early exit that uses Length(), or by a hash function that takes length as a parameter.
        // We don't have any system-wide mandatory early exits, and only some structures are hash based. It makes this the path of least resistance.
        bool needsLength = usedVisitor.Functions.HasFlag(GeneratorFunction.Length) || cacheHashInfo != null;
        AnnotatedExpr[] mandatoryExprs = needsLength ? GetMandatoryExpressions(inputKey).ToArray() : [];

        // Now we transform the expressions into more efficient representations
        AnnotatedExpr[] combinedExprs = mandatoryExprs.Concat(exprs).ToArray();
        AnnotatedExpr[] transformed = ExpressionHelper.Transform(combinedExprs,
        [
            new AllocationGatherTransform(),
            new DeduplicateAllocationTransform()
        ]).ToArray();

        if (cfg.EarlyExitConfig.Optimize)
            EarlyExitPipeline.OptimizeExpressions(transformed);

        foreach (AnnotatedExpr expr in transformed)
            usedVisitor.Visit(expr.Expression);

        // Visit the hash expression so that ReadU8/U16/U32/U64 helpers used by the hash are included in the generated source
        if (cacheHashInfo != null)
            usedVisitor.Visit(cacheHashInfo.Expression);

        StringGeneratorConfig genCfg = new StringGeneratorConfig(structureType, (uint)keys.Length, props.LengthData.LengthRanges.Min, props.LengthData.LengthRanges.Max, cfg.IgnoreCase, props.CharacterData.CharacterClasses, generator.Encoding, transformed, cfg.TypeReductionEnabled, cacheHashInfo, usedVisitor.Functions, requiredCapabilities);

        string source = generator.Generate<string, TValue>(genCfg, res);
        string[] earlyExitNames = earlyExits.Select(x => x.GetType().Name).ToArray();
        return new StringGenerationResult(source, earlyExitNames, stringHashName, structureType);

        IEnumerable<AnnotatedExpr> GetMandatoryExpressions(ParameterExpression key)
        {
            // This produces an allocation for length: int length = key.Length;
            // It is needed for length-based early exits and for hash functions, where length is passed as a parameter.
            // The caller guards this so it is only invoked when length is actually referenced.

            MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.Length), [typeof(string)])!;
            ParameterExpression length = Variable(typeof(int), "length");
            return [AnnotatedExpr.Allocation(Assign(length, Call(methodInfo, key)))];
        }

        (StructureType StructureType, IStructure<string, TValue, IContext> Structure, IContext Context) CreateSelectedStringStructure(StructureType selectedType)
        {
            ValidateCapabilities(selectedType, requiredCapabilities);
            IStructure<string, TValue, IContext> selectedStructure = StringStructureFactory<TValue>(selectedType, props, () => EnsureHashData(keys.Span), generator.Encoding);
            IContext? selectedContext = selectedStructure.Create(keys, values);

            if (selectedContext == null)
                throw new InvalidOperationException($"The selected structure {selectedType} failed to create.");

            return (selectedType, selectedStructure, selectedContext);
        }

        (StructureType StructureType, IStructure<string, TValue, IContext> Structure, IContext Context) CreateBestStringStructure()
        {
            StructureConfig structureConfig = cfg.StructureConfig.Clone();

            while (true)
            {
                StructureType selectedType = StringStructures.GetBest(keys, hasValues, props.LengthData.LengthRanges.Min, props.LengthData.LengthRanges.Max, cfg.AllowApproximation, props.LengthData.UniqueLengths, requiredCapabilities, structureConfig, x => EnsureHashData(x.Span));
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
            (cacheHashData, cacheHashInfo, cacheHashExits) = GetStringHash(keySpan);
            return cacheHashData;
        }

        (HashData, StringHashInfo, IEnumerable<IEarlyExit>) GetStringHash(ReadOnlySpan<string> keySpan)
        {
            IStringHash stringHash;

            if (cfg.StringAnalyzerConfig != null)
            {
                Candidate candidate = HashBenchmark.GetBestHash(keySpan, props, cfg.StringAnalyzerConfig, factory, generator.Encoding, true, cfg.IgnoreCase);
                LogStringHashFitness(logger, candidate.Fitness);
                stringHash = candidate.StringHash;
            }
            else
            {
                // When string analysis is disabled, we still produce an expression that needs compilation. This is because the hashes are produced in FastData's core
                // and the generated code's hash function must match, otherwise the generated hashes will be different from the runtime's hashes.
                stringHash = DefaultStringHash.GetInstance(generator.Encoding, cfg.IgnoreCase);
            }

            stringHashName = stringHash.GetType().Name;
            Expression<StringHashFunc> expression = stringHash.GetExpression();
            StringHashFunc hashFunc = expression.Compile();
            IEnumerable<IEarlyExit> hashExits = stringHash.GetMandatoryExits();

            Encoding encoding = StringHelper.GetEncoding(generator.Encoding);
            byte[] buffer = new byte[props.LengthData.MaxByteLength];

            HashData hashData = HashData.Create(keySpan,
                cfg.StructureSettings.GetSetting<float>(KnownSettings.HashTableCapacityFactor),
                cfg.StructureSettings.GetSetting<bool>(KnownSettings.OptimizeHashTableBucketSize),
                cfg.StructureSettings.GetSetting<bool>(KnownSettings.RoundModuloToPowerOfTwo),
                cfg.StructureSettings.GetSetting<float>(KnownSettings.RoundModuloToPowerOfTwoThreshold), x =>
                {
                    int byteCount = encoding.GetBytes(x, 0, x.Length, buffer, 0);
                    return hashFunc(buffer, byteCount);
                });

            return (hashData, new StringHashInfo(expression, stringHash.AdditionalData), hashExits);
        }
    }

    private static IStructure<string, TValue, IContext> StringStructureFactory<TValue>(StructureType type, StringKeyProperties props, Func<HashData> getHashData, GeneratorEncoding encoding) => type switch
    {
        StructureType.Array => new ArrayStructure<string, TValue>(),
        StructureType.BinarySearch => new BinarySearchStructure<string, TValue>(),
        StructureType.BloomFilter => new BloomFilterStructure<string, TValue>(getHashData()),
        StructureType.Conditional => new ConditionalStructure<string, TValue>(),
        StructureType.HashTable => new HashTableStructure<string, TValue>(getHashData()),
        StructureType.HashTableCompact => new HashTableCompactStructure<string, TValue>(getHashData()),
        StructureType.HashTablePerfect => new HashTablePerfectStructure<string, TValue>(getHashData()),
        StructureType.Hyble => new HybleStructure<string, TValue>(getHashData()),
        StructureType.KeyLength => new KeyLengthStructure<string, TValue>(props.LengthData.LengthRanges.Min, props.LengthData.LengthRanges.Max, encoding),
        StructureType.SingleValue => new SingleValueStructure<string, TValue>(),
        _ => throw new InvalidOperationException($"Unsupported DataStructure {type}")
    };

    private static NumericGenerationResult GenerateNumericInternal<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, bool hasValues, NumericDataConfig cfg, ICodeGenerator generator, ILoggerFactory? factory = null)
    {
        if (keys.IsEmpty)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (hasValues && keys.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        StructureCapability requiredCapabilities = GetRequiredCapabilities(cfg, hasValues);

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
        Deduplication.DeduplicateNumericKeys(cfg, ref keys, ref values);
        int newCount = keys.Length;

        if (oldCount == newCount) // No duplicates removed
            LogNumberOfKeys(logger, oldCount);
        else // Duplicates removed
            LogNumberOfUniqueKeys(logger, oldCount, newCount);

        NumericKeyProperties<TKey> props = KeyAnalyzer.GetNumericProperties(keys);
        LogMinMaxValues(logger, props.DataRanges.Min, props.DataRanges.Max);

        HashData? cacheHashData = null;

        StructureType? structureTypeOverride = cfg.StructureTypeOverride is {} value && value != StructureType.Auto ? value : null;
        (StructureType structureType, IStructure<TKey, TValue, IContext> structure, IContext ctx) = structureTypeOverride.HasValue ? CreateSelectedNumericStructure(structureTypeOverride.Value) : CreateBestNumericStructure();
        LogStructureType(logger, structureType.ToString());

        // Early exits are generated from numeric properties and then merged with checks required by the structure itself.
        IEarlyExit[] exitsAnalyzed = NumericEarlyExits<TKey>.GetExits(structureType, keys, props.DataRanges, props.Range, props.BitMask, (uint)keys.Length, cfg.EarlyExitConfig);
        List<IEarlyExit> exits = EarlyExitPipeline.CombineAndDedup(structure.GetMandatoryExits(), exitsAnalyzed);

        if (cfg.EarlyExitConfig.Optimize)
            EarlyExitPipeline.Optimize<TKey>(exits);

        // Convert the early exits into a set of annotated expressions. We assume the input is called "key".
        ParameterExpression inputKey = Parameter(typeof(TKey), "key");
        AnnotatedExpr[] exprs = EarlyExitPipeline.Annotate(exits, inputKey);

        if (cfg.EarlyExitConfig.Optimize)
            EarlyExitPipeline.OptimizeExpressions(exprs);

        NumericGeneratorConfig genCfg = new NumericGeneratorConfig(structureType, (uint)keys.Length, props.DataRanges.Min, props.DataRanges.Max, exprs, cfg.TypeReductionEnabled, props.HasZero, requiredCapabilities);

        string source = generator.Generate<TKey, TValue>(genCfg, ctx);
        string[] earlyExitNames = exits.Select(x => x.GetType().Name).ToArray();
        return new NumericGenerationResult(source, earlyExitNames, structureType);

        (StructureType StructureType, IStructure<TKey, TValue, IContext> Structure, IContext Context) CreateSelectedNumericStructure(StructureType selType)
        {
            ValidateCapabilities(selType, requiredCapabilities);
            IStructure<TKey, TValue, IContext> selStruct = NumericStructureFactory<TKey, TValue>(cfg, selType, props, () => EnsureNumericHash(keys.Span));
            IContext? selCtx = selStruct.Create(keys, values);

            return selCtx == null ? throw new InvalidOperationException($"The selected structure {selType} failed to create.") : (selType, selStruct, selCtx);
        }

        (StructureType StructureType, IStructure<TKey, TValue, IContext> Structure, IContext Context) CreateBestNumericStructure()
        {
            StructureConfig structureConfig = cfg.StructureConfig.Clone();

            while (true)
            {
                StructureType selType = NumericStructures<TKey>.GetBest(keys, hasValues, props.Density, cfg.AllowApproximation, props.DataRanges.Ranges.Count, props.Range, requiredCapabilities,
                    cfg.StructureSettings.GetSetting<float>(KnownSettings.DenseIntegralValueMaxRangeFactor), structureConfig, x => EnsureNumericHash(x.Span));
                IStructure<TKey, TValue, IContext> selStruct = NumericStructureFactory<TKey, TValue>(cfg, selType, props, () => EnsureNumericHash(keys.Span));
                IContext? selCtx = selStruct.Create(keys, values);

                if (selCtx != null)
                    return (selType, selStruct, selCtx);

                structureConfig.Disable(selType);
            }
        }

        HashData EnsureNumericHash(ReadOnlySpan<TKey> keySpan) => cacheHashData ??= GetNumericHash(keySpan);

        HashData GetNumericHash(ReadOnlySpan<TKey> keySpan)
        {
            NumericHashFunc<TKey> hashFunc = DefaultNumericHash.GetHashFunc<TKey>(props.HasZero);
            return HashData.Create(keySpan,
                cfg.StructureSettings.GetSetting<float>(KnownSettings.HashTableCapacityFactor),
                cfg.StructureSettings.GetSetting<bool>(KnownSettings.OptimizeHashTableBucketSize),
                cfg.StructureSettings.GetSetting<bool>(KnownSettings.RoundModuloToPowerOfTwo),
                cfg.StructureSettings.GetSetting<float>(KnownSettings.RoundModuloToPowerOfTwoThreshold), hashFunc);
        }
    }

    private static IStructure<TKey, TValue, IContext> NumericStructureFactory<TKey, TValue>(DataConfig cfg, StructureType type, NumericKeyProperties<TKey> props, Func<HashData> getHashData) => type switch
    {
        StructureType.Array => new ArrayStructure<TKey, TValue>(),
        StructureType.BinarySearch => new BinarySearchStructure<TKey, TValue>(),
        StructureType.BinarySearchInterpolation => new BinarySearchInterpolationStructure<TKey, TValue>(),
        StructureType.BitSet => new BitSetStructure<TKey, TValue>(props),
        StructureType.BloomFilter => new BloomFilterStructure<TKey, TValue>(getHashData()),
        StructureType.Conditional => new ConditionalStructure<TKey, TValue>(),
        StructureType.EliasFano => new EliasFanoStructure<TKey, TValue>(props.DataRanges.Min, props.DataRanges.Max, cfg.StructureSettings.GetSetting<int>(KnownSettings.EliasFanoSkipQuantum)),
        StructureType.HashTable => new HashTableStructure<TKey, TValue>(getHashData()),
        StructureType.HashTableCompact => new HashTableCompactStructure<TKey, TValue>(getHashData()),
        StructureType.HashTablePerfect => new HashTablePerfectStructure<TKey, TValue>(getHashData()),
        StructureType.Hyble => new HybleStructure<TKey, TValue>(getHashData()),
        StructureType.Range => new RangeStructure<TKey, TValue>(props.DataRanges),
        StructureType.RrrBitVector => new RrrBitVectorStructure<TKey, TValue>(props.DataRanges.Min, props.DataRanges.Max),
        StructureType.SingleValue => new SingleValueStructure<TKey, TValue>(),
        StructureType.Pgm => new PgmStructure<TKey, TValue>(cfg.StructureSettings.GetSetting<int>(KnownSettings.PgmEpsilon), cfg.StructureSettings.GetSetting<int>(KnownSettings.PgmEpsilonRecursive)),
        _ => throw new InvalidOperationException($"Unsupported DataStructure {type}")
    };

    private static StructureCapability GetRequiredCapabilities(DataConfig cfg, bool hasValues)
    {
        if (!hasValues && cfg.RequiredCapability.HasFlag(StructureCapability.KeyValueLookup))
            throw new InvalidOperationException("KeyValueLookup requires values. Use GenerateKeyed or provide a values file.");

        StructureCapability capabilities = cfg.RequiredCapability | StructureCapability.Membership;

        if (hasValues)
            capabilities |= StructureCapability.KeyValueLookup;

        return capabilities;
    }

    private static void ValidateCapabilities(StructureType structureType, StructureCapability requiredCapabilities)
    {
        if (StructureCapabilityHelper.Supports(structureType, requiredCapabilities))
            return;

        StructureCapability capabilities = StructureCapabilityHelper.GetStructureCapability(structureType);
        throw new InvalidOperationException($"Structure {structureType} does not support the required functions {requiredCapabilities}. Supported functions: {capabilities}.");
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

public readonly record struct StringGenerationResult(string Source, string[] EarlyExits, string? StringHashName, StructureType StructureType);
public readonly record struct NumericGenerationResult(string Source, string[] EarlyExits, StructureType StructureType);