using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Helpers;

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
            return RunProcess(application, args) == 0;
        }
        catch
        {
            return false;
        }
    }

    public static int RunProcess(string application, string? args = null, string? workingDir = null)
    {
        using Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = application,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = workingDir
        };

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
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

    /// <summary>This variant of Generate bypasses the public API to test more advanced combinations of parameters</summary>
    public static GeneratorSpec Generate<T>(Func<string, ICodeGenerator> func, TestVector<T> vector)
    {
        DataType dataType = Enum.Parse<DataType>(typeof(T).Name);

        IProperties props;

        if (vector.Values is string[] arr)
            props = KeyAnalyzer.GetStringProperties(arr);
        else
            props = KeyAnalyzer.GetValueProperties(vector.Values);

        ICodeGenerator generator = func(vector.Identifier);
        GeneratorEncoding encoding = generator.Encoding;

        byte[] values = new byte[vector.Values.Length];
        Array.Fill(values, (byte)42);

        if (vector.Type == typeof(SingleValueStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.Auto, new SingleValueStructure<T, byte>(), values);
        if (vector.Type == typeof(ArrayStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.Array, new ArrayStructure<T, byte>(), values);
        if (vector.Type == typeof(ConditionalStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.Conditional, new ConditionalStructure<T, byte>(), values);
        if (vector.Type == typeof(BinarySearchStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.BinarySearch, new BinarySearchStructure<T, byte>(dataType, StringComparison.Ordinal), values);
        if (vector.Type == typeof(EytzingerSearchStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.BinarySearch, new EytzingerSearchStructure<T, byte>(dataType, StringComparison.Ordinal), values);
        if (vector.Type == typeof(HashTableChainStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.HashTable, new HashTableChainStructure<T, byte>(GetHashData(vector, dataType, encoding), dataType), values);
        if (vector.Type == typeof(HashTablePerfectStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.HashTable, new HashTablePerfectStructure<T, byte>(GetHashData(vector, dataType, encoding), dataType), values);
        if (vector.Type == typeof(KeyLengthStructure<,>))
            return Generate(generator, vector, props, dataType, StructureType.Auto, new KeyLengthStructure<T, byte>((StringProperties)props), values);

        throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);
    }

    private static HashData GetHashData<T>(TestVector<T> vector, DataType dataType, GeneratorEncoding genEnc)
    {
        HashData hashData;

        if (dataType == DataType.String)
        {
            Encoding encoding = genEnc == GeneratorEncoding.UTF8 ? Encoding.UTF8 : Encoding.Unicode;
            StringHashFunc func = DefaultStringHash.GetInstance(genEnc).GetExpression().Compile();

            hashData = HashData.Create(vector.Values, 1, obj =>
            {
                byte[] data = encoding.GetBytes((string)(object)obj);
                return func(data, data.Length);
            });
        }
        else
            hashData = HashData.Create(vector.Values, 1, PrimitiveHash.GetHash<T>(dataType, false));

        return hashData;
    }

    private static GeneratorSpec Generate<TKey, TValue, TContext>(ICodeGenerator generator, TestVector<TKey> vector, IProperties props, DataType dataType, StructureType structureType, IStructure<TKey, TValue, TContext> structure, TValue[]? value) where TContext : IContext
    {
        TContext context = structure.Create(vector.Values, value);

        GeneratorConfig<TKey> genCfg;
        HashDetails hashDetails = new HashDetails();

        GeneratorFlags flags = GeneratorFlags.None;

        if (props is StringProperties stringProps)
        {
            if (stringProps.CharacterData.AllAscii)
                flags = GeneratorFlags.AllAreASCII;

            genCfg = new GeneratorConfig<TKey>(structureType, dataType, (uint)vector.Values.Length, stringProps, StringComparison.Ordinal, hashDetails, generator.Encoding, flags);
        }
        else if (props is ValueProperties<TKey> valueProps)
        {
            hashDetails.HasZeroOrNaN = valueProps.HasZeroOrNaN;
            genCfg = new GeneratorConfig<TKey>(structureType, dataType, (uint)vector.Values.Length, valueProps, hashDetails, flags);
        }
        else
            throw new InvalidOperationException("Bug");

        string source = generator.Generate<TKey, TValue>(genCfg, context);
        return new GeneratorSpec(vector.Identifier, source, flags);
    }
}