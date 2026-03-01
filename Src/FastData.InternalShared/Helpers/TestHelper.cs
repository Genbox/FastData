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

namespace Genbox.FastData.InternalShared.Helpers;

public static class TestHelper
{
    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRandomString(Random rng, int length)
    {
        char[] data = new char[length];

        for (int i = 0; i < length; i++)
        {
            data[i] = _alphabet[rng.Next(0, _alphabet.Length)];
        }

        return new string(data);
    }

    public static GeneratorSpec Generate<TKey>(Func<string, ICodeGenerator> func, TestVector<TKey> vector) => GenerateInternal(func, vector, ReadOnlyMemory<byte>.Empty);

    public static GeneratorSpec Generate<TKey, TValue>(Func<string, ICodeGenerator> func, TestVector<TKey, TValue> vector) where TValue : notnull => GenerateInternal(func, vector, (ReadOnlyMemory<TValue>)vector.Values);

    private static GeneratorSpec GenerateInternal<TKey, TValue>(Func<string, ICodeGenerator> func, TestVector<TKey> vector, ReadOnlyMemory<TValue> values) where TValue : notnull
    {
        ReadOnlyMemory<TKey> keyMemory = vector.Keys;

        if (keyMemory.Length == 0)
            throw new InvalidOperationException("No data provided. Please provide at least one item to generate code for.");

        if (!values.IsEmpty && keyMemory.Length != values.Length)
            throw new InvalidOperationException("The number of values does not match the number of keys.");

        ReadOnlySpan<TKey> keySpan = keyMemory.Span;

        //Sanity check to avoid duplicate keys in the original input
        if (typeof(TKey) == typeof(string))
        {
            ReadOnlyMemory<string> stringMemory = CastMemory<TKey, string>(keyMemory);
            ReadOnlySpan<string> stringSpan = stringMemory.Span;
            HashSet<string> uniq = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < stringSpan.Length; i++)
            {
                string key = stringSpan[i];
                if (!uniq.Add(key))
                    throw new InvalidOperationException($"Duplicate key found: {key}");
            }
        }
        else
        {
            HashSet<TKey> uniq = new HashSet<TKey>(keySpan.Length);

            for (int i = 0; i < keySpan.Length; i++)
            {
                TKey key = keySpan[i];
                if (!uniq.Add(key))
                    throw new InvalidOperationException($"Duplicate key found: {key}");
            }
        }

        ICodeGenerator generator = func(vector.Identifier);

        IProperties props;
        string trimPrefix = string.Empty;
        string trimSuffix = string.Empty;

        FastDataConfig config = new FastDataConfig
        {
            IgnoreCase = false,
            EnablePrefixSuffixTrimming = true
        };

        if (typeof(TKey) == typeof(string))
        {
            ReadOnlyMemory<string> stringMemory = CastMemory<TKey, string>(keyMemory);
            ReadOnlySpan<string> stringSpan = stringMemory.Span;
            StringKeyProperties strProps = KeyAnalyzer.GetStringProperties(stringSpan, config.EnablePrefixSuffixTrimming, config.IgnoreCase, generator.Encoding);

            if (strProps.DeltaData.Prefix.Length > 0 || strProps.DeltaData.Suffix.Length > 0)
            {
                trimPrefix = strProps.DeltaData.Prefix;
                trimSuffix = strProps.DeltaData.Suffix;

                stringMemory = FastDataGenerator.SubStringKeys(stringSpan, strProps);
                keyMemory = CastMemory<string, TKey>(stringMemory);
                keySpan = keyMemory.Span;
            }

            props = strProps;
        }
        else
            props = KeyAnalyzer.GetNumericProperties(keyMemory, false);

        TempState<TKey, TValue> state = new TempState<TKey, TValue>(keyMemory, values, vector, generator, props, trimPrefix, trimSuffix, config);

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

    private static ReadOnlyMemory<TTo> CastMemory<TFrom, TTo>(ReadOnlyMemory<TFrom> memory) => (ReadOnlyMemory<TTo>)(object)memory;

    private static GeneratorSpec Generate<TKey, TValue, TContext>(in TempState<TKey, TValue> state, IStructure<TKey, TValue, TContext> structure) where TContext : IContext
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

        string source = state.Generator.Generate<TKey, TValue>(genCfg, context);
        return new GeneratorSpec(state.Vector.Identifier, source);
    }

    private readonly record struct TempState<TKey, TValue>(ReadOnlyMemory<TKey> Keys, ReadOnlyMemory<TValue> Values, TestVector<TKey> Vector, ICodeGenerator Generator, IProperties KeyProperties, string TrimPrefix, string TrimSuffix, FastDataConfig Config);
}