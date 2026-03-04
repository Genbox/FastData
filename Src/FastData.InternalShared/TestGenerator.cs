using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared;

public static class TestGenerator
{
    public static string Generate<TKey>(ICodeGenerator generator, TestVector<TKey> vector) => GenerateInternal(generator, vector, ReadOnlyMemory<byte>.Empty);

    public static string Generate<TKey, TValue>(ICodeGenerator generator, TestVector<TKey, TValue> vector) where TValue : notnull => GenerateInternal(generator, vector, (ReadOnlyMemory<TValue>)vector.Values);

    private static string GenerateInternal<TKey, TValue>(ICodeGenerator generator, TestVector<TKey> vector, ReadOnlyMemory<TValue> values) where TValue : notnull
    {
        ReadOnlyMemory<TKey> keyMemory = vector.Keys;

        if (keyMemory.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (!values.IsEmpty && keyMemory.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        IProperties props;
        FastDataConfig config = new FastDataConfig();

        if (typeof(TKey) == typeof(string))
        {
            StringKeyProperties strProps = KeyAnalyzer.GetStringProperties(((ReadOnlyMemory<string>)(object)keyMemory).Span, false, false, generator.Encoding);
            props = strProps;
        }
        else
            props = KeyAnalyzer.GetNumericProperties(keyMemory, false);

        TempState<TKey, TValue> state = new TempState<TKey, TValue>(keyMemory, values, vector, generator, props, string.Empty, string.Empty, config);
        ReadOnlySpan<TKey> keySpan = keyMemory.Span;

        if (vector.Type == typeof(SingleValueStructure<,>))
            return Generate(state, new SingleValueStructure<TKey, TValue>());
        if (vector.Type == typeof(ArrayStructure<,>))
            return Generate(state, new ArrayStructure<TKey, TValue>());
        if (vector.Type == typeof(ConditionalStructure<,>))
            return Generate(state, new ConditionalStructure<TKey, TValue>());
        if (vector.Type == typeof(BinarySearchStructure<,>))
            return Generate(state, new BinarySearchStructure<TKey, TValue>(config.IgnoreCase));
        if (vector.Type == typeof(BinarySearchInterpolationStructure<,>))
            return Generate(state, new BinarySearchInterpolationStructure<TKey, TValue>());
        if (vector.Type == typeof(HashTableStructure<,>))
            return Generate(state, new HashTableStructure<TKey, TValue>(GetHashData(keySpan, generator.Encoding)));
        if (vector.Type == typeof(HashTablePerfectStructure<,>))
            return Generate(state, new HashTablePerfectStructure<TKey, TValue>(GetHashData(keySpan, generator.Encoding)));
        if (vector.Type == typeof(KeyLengthStructure<,>))
            return Generate(state, new KeyLengthStructure<TKey, TValue>((StringKeyProperties)props));
        if (vector.Type == typeof(RangeStructure<,>))
            return Generate(state, new RangeStructure<TKey, TValue>((NumericKeyProperties<TKey>)props));
        if (vector.Type == typeof(BitSetStructure<,>))
            return Generate(state, new BitSetStructure<TKey, TValue>((NumericKeyProperties<TKey>)props));
        if (vector.Type == typeof(HashTableCompactStructure<,>))
            return Generate(state, new HashTableCompactStructure<TKey, TValue>(GetHashData(keySpan, generator.Encoding)));
        if (vector.Type == typeof(BloomFilterStructure<,>))
            return Generate(state, new BloomFilterStructure<TKey, TValue>(GetHashData(keySpan, generator.Encoding)));
        if (vector.Type == typeof(EliasFanoStructure<,>))
            return Generate(state, new EliasFanoStructure<TKey, TValue>((NumericKeyProperties<TKey>)props, config));
        if (vector.Type == typeof(RrrBitVectorStructure<,>))
            return Generate(state, new RrrBitVectorStructure<TKey, TValue>());

        throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);
    }

    private static HashData GetHashData<T>(ReadOnlySpan<T> keys, GeneratorEncoding genEnc)
    {
        HashData hashData;

        if (typeof(T) == typeof(string))
        {
            Encoding encoding = genEnc == GeneratorEncoding.UTF8 ? Encoding.UTF8 : Encoding.Unicode;
            StringHashFunc func = DefaultStringHash.GetInstance(genEnc).GetExpression().Compile();

            hashData = HashData.Create(keys, 1, obj =>
            {
                byte[] data = encoding.GetBytes((string)(object)obj);
                return func(data, data.Length);
            });
        }
        else
            hashData = HashData.Create(keys, 1, PrimitiveHash.GetHashFunc<T>(false));

        return hashData;
    }

    private static string Generate<TKey, TValue, TContext>(in TempState<TKey, TValue> state, IStructure<TKey, TValue, TContext> structure) where TContext : IContext
    {
        TContext context = structure.Create(state.Keys, state.Values);

        GeneratorConfigBase genCfg;
        HashDetails hashDetails = new HashDetails();

        if (state.KeyProperties is StringKeyProperties stringProps)
        {
            genCfg = new StringGeneratorConfig(state.Vector.Type, (uint)state.Keys.Length, stringProps, hashDetails, state.Generator.Encoding, state.TrimPrefix, state.TrimSuffix, state.Config);
        }
        else if (state.KeyProperties is NumericKeyProperties<TKey> valueProps)
        {
            hashDetails.HasZeroOrNaN = valueProps.HasZeroOrNaN;
            genCfg = new NumericGeneratorConfig<TKey>(state.Vector.Type, (uint)state.Keys.Length, valueProps, hashDetails, state.Config);
        }
        else
            throw new InvalidOperationException("Bug");

        return state.Generator.Generate<TKey, TValue>(genCfg, context);
    }

    private readonly record struct TempState<TKey, TValue>(ReadOnlyMemory<TKey> Keys, ReadOnlyMemory<TValue> Values, TestVector<TKey> Vector, ICodeGenerator Generator, IProperties KeyProperties, string TrimPrefix, string TrimSuffix, FastDataConfig Config);
}