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
    public static GeneratorSpec Generate<T>(Func<string, ICodeGenerator> func, TestVector<T> vector) where T : notnull
    {
        DataType dataType = Enum.Parse<DataType>(typeof(T).Name);

        IProperties props;
        ReadOnlySpan<T> span = vector.Values.AsReadOnlySpan();

        if (typeof(T) == typeof(string))
            props = DataAnalyzer.GetStringProperties(span);
        else
            props = DataAnalyzer.GetValueProperties(span, dataType);

        if (vector.Type == typeof(SingleValueStructure<>))
            return Generate(func, vector, props, dataType, StructureType.Auto, new SingleValueStructure<T>());
        if (vector.Type == typeof(ArrayStructure<>))
            return Generate(func, vector, props, dataType, StructureType.Array, new ArrayStructure<T>());
        if (vector.Type == typeof(ConditionalStructure<>))
            return Generate(func, vector, props, dataType, StructureType.Conditional, new ConditionalStructure<T>());
        if (vector.Type == typeof(BinarySearchStructure<>))
            return Generate(func, vector, props, dataType, StructureType.BinarySearch, new BinarySearchStructure<T>(dataType, StringComparison.Ordinal));
        if (vector.Type == typeof(EytzingerSearchStructure<>))
            return Generate(func, vector, props, dataType, StructureType.BinarySearch, new EytzingerSearchStructure<T>(dataType, StringComparison.Ordinal));
        if (vector.Type == typeof(HashSetChainStructure<>))
        {
            HashFunc<T> hashFunc = dataType == DataType.String ? (HashFunc<T>)(object)new DefaultStringHash().GetHashFunction() : PrimitiveHash.GetHash<T>(dataType, false);
            return Generate(func, vector, props, dataType, StructureType.HashSet, new HashSetChainStructure<T>(HashData.Create(vector.Values.AsSpan(), 1, hashFunc), dataType));
        }
        if (vector.Type == typeof(HashSetPerfectStructure<>))
        {
            HashFunc<T> hashFunc = dataType == DataType.String ? (HashFunc<T>)(object)new DefaultStringHash().GetHashFunction() : PrimitiveHash.GetHash<T>(dataType, false);
            return Generate(func, vector, props, dataType, StructureType.HashSet, new HashSetPerfectStructure<T>(HashData.Create(vector.Values.AsSpan(), 1, hashFunc), dataType));
        }
        if (vector.Type == typeof(HashSetLinearStructure<>))
        {
            HashFunc<T> hashFunc = dataType == DataType.String ? (HashFunc<T>)(object)new DefaultStringHash().GetHashFunction() : PrimitiveHash.GetHash<T>(dataType, false);
            return Generate(func, vector, props, dataType, StructureType.HashSet, new HashSetLinearStructure<T>(HashData.Create(vector.Values.AsSpan(), 1, hashFunc)));
        }
        if (vector.Type == typeof(KeyLengthStructure<>))
            return Generate(func, vector, props, dataType, StructureType.Auto, new KeyLengthStructure<T>((StringProperties)props));

        throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);
    }

    private static GeneratorSpec Generate<T, TContext>(Func<string, ICodeGenerator> func, TestVector<T> vector, IProperties props, DataType dataType, StructureType structureType, IStructure<T, TContext> structure) where T : notnull where TContext : IContext<T>
    {
        ReadOnlySpan<T> values = vector.Values.AsReadOnlySpan();
        TContext context = structure.Create(ref values);
        ICodeGenerator generator = func(vector.Identifier);

        GeneratorConfig<T> genCfg;
        HashDetails hashDetails = new HashDetails();

        if (props is StringProperties stringProps)
            genCfg = new GeneratorConfig<T>(structureType, dataType, (uint)vector.Values.Length, stringProps, StringComparison.Ordinal, hashDetails);
        else if (props is ValueProperties<T> valueProps)
        {
            hashDetails.HasZeroOrNaN = valueProps.HasZeroOrNaN;
            genCfg = new GeneratorConfig<T>(structureType, dataType, (uint)vector.Values.Length, valueProps, hashDetails);
        }
        else
            throw new InvalidOperationException("Bug");

        string source = generator.Generate(values, genCfg, context);
        return new GeneratorSpec(vector.Identifier, source);
    }
}