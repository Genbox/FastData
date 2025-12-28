using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
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

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public static class TestHelper
{
    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(false);

    public static void CreateOrEmptyDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            foreach (string file in Directory.EnumerateFiles(path))
                File.Delete(file);

            foreach (string dir in Directory.EnumerateDirectories(path))
                Directory.Delete(dir, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    public static string GenerateRandomString(Random rng, int length)
    {
        char[] data = new char[length];

        for (int i = 0; i < length; i++)
        {
            data[i] = _alphabet[rng.Next(0, _alphabet.Length)];
        }

        return new string(data);
    }

    public static bool TryRunProcess(string application, string args)
    {
        try
        {
            return RunProcess(application, args).ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [SuppressMessage("Usage", "MA0040:Forward the CancellationToken parameter to methods that take one")]
    public static ProcessResult RunProcess(string application, string? args = null, string? workingDir = null, int timeoutMs = 5000)
    {
        using Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = application,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process.Start();

        Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

        bool exited = process.WaitForExit(timeoutMs);

        if (!exited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore - best effort.
            }

            try
            {
                process.WaitForExit(1000);
            }
            catch
            {
                // Ignore - best effort.
            }
        }

        string stdOut = "";
        string stdErr = "";

        try { stdOut = stdOutTask.GetAwaiter().GetResult(); }
        catch
        {
            /* best effort */
        }
        try { stdErr = stdErrTask.GetAwaiter().GetResult(); }
        catch
        {
            /* best effort */
        }

        return new ProcessResult(process.ExitCode, stdOut, stdErr);
    }

    /// <summary>This function is here to help avoid write-fatigue</summary>
    /// <returns>True if the file was written, false if it was skipped due to identical content</returns>
    public static bool TryWriteFile(string path, string content)
    {
        if (File.Exists(path))
        {
            byte[] oldHash = SHA1.HashData(File.ReadAllBytes(path));
            byte[] newHash = SHA1.HashData(_utf8NoBom.GetBytes(content));

            if (oldHash.SequenceEqual(newHash))
                return false;
        }

        File.WriteAllText(path, content, _utf8NoBom);
        return true;
    }

    public static GeneratorSpec Generate<TKey>(Func<string, ICodeGenerator> func, TestVector<TKey> vector, bool ignoreCase = false) => GenerateInternal(func, vector, ReadOnlyMemory<byte>.Empty, ignoreCase);

    public static GeneratorSpec Generate<TKey, TValue>(Func<string, ICodeGenerator> func, TestVector<TKey, TValue> vector, bool ignoreCase = false) where TValue : notnull => GenerateInternal(func, vector, (ReadOnlyMemory<TValue>)vector.Values, ignoreCase);

    private static GeneratorSpec GenerateInternal<TKey, TValue>(Func<string, ICodeGenerator> func, TestVector<TKey> vector, ReadOnlyMemory<TValue> values, bool ignoreCase) where TValue : notnull
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

        KeyType keyType;
        if (typeof(TKey) == typeof(string))
            keyType = KeyType.String;
        else
        {
            Type type = typeof(TKey);

            if (type != typeof(char) && type != typeof(sbyte) && type != typeof(byte) && type != typeof(short) && type != typeof(ushort) && type != typeof(int) && type != typeof(uint) && type != typeof(long) && type != typeof(ulong) && type != typeof(float) && type != typeof(double))
                throw new InvalidOperationException($"Unsupported data type: {type.Name}");

            keyType = Enum.Parse<KeyType>(type.Name);
        }

        ICodeGenerator generator = func(vector.Identifier);

        IProperties props;
        string trimPrefix = string.Empty;
        string trimSuffix = string.Empty;
        if (typeof(TKey) == typeof(string))
        {
            ReadOnlyMemory<string> stringMemory = CastMemory<TKey, string>(keyMemory);
            ReadOnlySpan<string> stringSpan = stringMemory.Span;
            StringKeyProperties strProps = KeyAnalyzer.GetStringProperties(stringSpan, true, ignoreCase, generator.Encoding); // Enable trimming

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
            props = KeyAnalyzer.GetNumericProperties(keyMemory);

        GeneratorEncoding encoding = generator.Encoding;
        TempState<TKey, TValue> state = new TempState<TKey, TValue>(keyMemory, values, keyType, vector, generator, props, trimPrefix, trimSuffix, ignoreCase);

        if (vector.Type == typeof(SingleValueStructure<,>))
            return Generate(state, new SingleValueStructure<TKey, TValue>());
        if (vector.Type == typeof(ArrayStructure<,>))
            return Generate(state, new ArrayStructure<TKey, TValue>());
        if (vector.Type == typeof(ConditionalStructure<,>))
            return Generate(state, new ConditionalStructure<TKey, TValue>());
        if (vector.Type == typeof(BinarySearchStructure<,>))
            return Generate(state, new BinarySearchStructure<TKey, TValue>(keyType, ignoreCase));
        if (vector.Type == typeof(HashTableStructure<,>))
            return Generate(state, new HashTableStructure<TKey, TValue>(GetHashData(keySpan, keyType, encoding), keyType));
        if (vector.Type == typeof(HashTablePerfectStructure<,>))
            return Generate(state, new HashTablePerfectStructure<TKey, TValue>(GetHashData(keySpan, keyType, encoding), keyType));
        if (vector.Type == typeof(KeyLengthStructure<,>))
            return Generate(state, new KeyLengthStructure<TKey, TValue>((StringKeyProperties)props));
        if (vector.Type == typeof(RangeStructure<,>))
            return Generate(state, new RangeStructure<TKey, TValue>((NumericKeyProperties<TKey>)props));
        if (vector.Type == typeof(BitSetStructure<,>))
            return Generate(state, new BitSetStructure<TKey, TValue>((NumericKeyProperties<TKey>)props, keyType));
        if (vector.Type == typeof(HashTableCompactStructure<,>))
            return Generate(state, new HashTableCompactStructure<TKey, TValue>(GetHashData(keySpan, keyType, encoding), keyType));

        throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);
    }

    private static HashData GetHashData<T>(ReadOnlySpan<T> keys, KeyType keyType, GeneratorEncoding genEnc)
    {
        HashData hashData;

        if (keyType == KeyType.String)
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
            hashData = HashData.Create(keys, 1, PrimitiveHash.GetHash<T>(keyType, false));

        return hashData;
    }

    private static ReadOnlyMemory<TTo> CastMemory<TFrom, TTo>(ReadOnlyMemory<TFrom> memory) => (ReadOnlyMemory<TTo>)(object)memory;

    private static GeneratorSpec Generate<TKey, TValue, TContext>(in TempState<TKey, TValue> state, IStructure<TKey, TValue, TContext> structure) where TContext : IContext<TValue>
    {
        TContext context = structure.Create(state.Keys, state.Values);

        GeneratorConfig<TKey> genCfg;
        HashDetails hashDetails = new HashDetails();

        GeneratorFlags flags = GeneratorFlags.None;

        if (state.KeyProperties is StringKeyProperties stringProps)
        {
            if (stringProps.CharacterData.AllAscii)
                flags = GeneratorFlags.AllAreASCII;

            genCfg = new GeneratorConfig<TKey>(state.Vector.Type, state.KeyType, (uint)state.Keys.Length, stringProps, state.IgnoreCase, hashDetails, state.Generator.Encoding, flags, state.TrimPrefix, state.TrimSuffix);
        }
        else if (state.KeyProperties is NumericKeyProperties<TKey> valueProps)
        {
            hashDetails.HasZeroOrNaN = valueProps.HasZeroOrNaN;
            genCfg = new GeneratorConfig<TKey>(state.Vector.Type, state.KeyType, (uint)state.Keys.Length, valueProps, hashDetails, flags);
        }
        else
            throw new InvalidOperationException("Bug");

        string source = state.Generator.Generate(genCfg, context);
        return new GeneratorSpec(state.Vector.Identifier, source, flags);
    }

    private readonly record struct TempState<TKey, TValue>(ReadOnlyMemory<TKey> Keys, ReadOnlyMemory<TValue> Values, KeyType KeyType, TestVector<TKey> Vector, ICodeGenerator Generator, IProperties KeyProperties, string TrimPrefix, string TrimSuffix, bool IgnoreCase);
}