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

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);

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

        return new ProcessResult(process.ExitCode, stdOut, stdErr, TimedOut: !exited);
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

    public static GeneratorSpec Generate<TKey>(Func<string, ICodeGenerator> func, TestVector<TKey> vector) => Generate<TKey, byte>(func, vector, null);

    public static GeneratorSpec Generate<TKey, TValue>(Func<string, ICodeGenerator> func, TestVector<TKey, TValue> vector) where TValue : notnull => Generate(func, vector, vector.Values);

    /// <summary>This variant of Generate bypasses the public API to test more advanced combinations of parameters</summary>
    public static GeneratorSpec Generate<TKey, TValue>(Func<string, ICodeGenerator> func, TestVector<TKey> vector, TValue[]? values) where TValue : notnull
    {
        TKey[] keys = vector.Keys;
        string? trimPrefix = null;
        string? trimSuffix = null;

        //Sanity check to avoid duplicate keys in the original input
        HashSet<TKey> uniq = new HashSet<TKey>(keys.Length);

        foreach (TKey key in keys)
        {
            if (!uniq.Add(key))
                throw new InvalidOperationException($"Duplicate key found: {key}");
        }

        KeyType keyType = Enum.Parse<KeyType>(typeof(TKey).Name);

        IProperties props;
        if (keys is string[] strKeys)
        {
            StringProperties strProps = KeyAnalyzer.GetStringProperties(strKeys, true); // Enable trimming

            if (strProps.DeltaData.LeftZeroCount > 0 || strProps.DeltaData.RightZeroCount > 0)
            {
                if (strProps.DeltaData.LeftZeroCount > 0)
                    trimPrefix = strKeys[0].Substring(0, strProps.DeltaData.LeftZeroCount);

                if (strProps.DeltaData.RightZeroCount > 0)
                    trimSuffix = strKeys[0].Substring(strProps.DeltaData.RightZeroCount);

                keys = (TKey[])(object)FastDataGenerator.SubStringKeys(strKeys, strProps);
            }

            props = strProps;
        }
        else
            props = KeyAnalyzer.GetProperties(keys);

        ICodeGenerator generator = func(vector.Identifier);
        GeneratorEncoding encoding = generator.Encoding;

        if (vector.Type == typeof(SingleValueStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.Auto, new SingleValueStructure<TKey, TValue>(), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(ArrayStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.Array, new ArrayStructure<TKey, TValue>(), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(ConditionalStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.Conditional, new ConditionalStructure<TKey, TValue>(), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(BinarySearchStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.BinarySearch, new BinarySearchStructure<TKey, TValue>(keyType, StringComparison.Ordinal), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(HashTableStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.HashTable, new HashTableStructure<TKey, TValue>(GetHashData(keys, keyType, encoding), keyType), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(HashTablePerfectStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.HashTable, new HashTablePerfectStructure<TKey, TValue>(GetHashData(keys, keyType, encoding), keyType), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(KeyLengthStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.Auto, new KeyLengthStructure<TKey, TValue>((StringProperties)props), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(RangeStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.Auto, new RangeStructure<TKey, TValue>(), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(BitSetStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.Auto, new BitSetStructure<TKey, TValue>((KeyProperties<TKey>)props, keyType), keys, values, trimPrefix, trimSuffix);
        if (vector.Type == typeof(HashTableCompactStructure<,>))
            return Generate(generator, vector, props, keyType, StructureType.Auto, new HashTableCompactStructure<TKey, TValue>(GetHashData(keys, keyType, encoding), keyType), keys, values, trimPrefix, trimSuffix);

        throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);
    }

    private static HashData GetHashData<T>(T[] keys, KeyType keyType, GeneratorEncoding genEnc)
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

    private static GeneratorSpec Generate<TKey, TValue, TContext>(ICodeGenerator generator, TestVector<TKey> vector, IProperties props, KeyType keyType, StructureType structureType, IStructure<TKey, TValue, TContext> structure, TKey[] keys, TValue[]? values, string? trimPrefix, string? trimSuffix) where TContext : IContext<TValue>
    {
        TContext context = structure.Create(keys, values);

        GeneratorConfig<TKey> genCfg;
        HashDetails hashDetails = new HashDetails();

        GeneratorFlags flags = GeneratorFlags.None;

        if (props is StringProperties stringProps)
        {
            if (stringProps.CharacterData.AllAscii)
                flags = GeneratorFlags.AllAreASCII;

            genCfg = new GeneratorConfig<TKey>(structureType, keyType, (uint)keys.Length, stringProps, StringComparison.Ordinal, hashDetails, generator.Encoding, flags, trimPrefix, trimSuffix);
        }
        else if (props is KeyProperties<TKey> valueProps)
        {
            hashDetails.HasZeroOrNaN = valueProps.HasZeroOrNaN;
            genCfg = new GeneratorConfig<TKey>(structureType, keyType, (uint)keys.Length, valueProps, hashDetails, flags);
        }
        else
            throw new InvalidOperationException("Bug");

        string source = generator.Generate(genCfg, context);
        return new GeneratorSpec(vector.Identifier, source, flags);
    }
}