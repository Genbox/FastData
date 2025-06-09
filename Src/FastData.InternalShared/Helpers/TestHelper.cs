using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Abstracts;
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
            Directory.Delete(path, true);

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
        DataProperties<T> props = DataProperties<T>.Create(vector.Values);
        ReadOnlySpan<T> data = vector.Values.AsSpan();

        if (vector.Type == typeof(SingleValueStructure<>))
            return Generate(func, props, vector, new SingleValueStructure<T>());
        if (vector.Type == typeof(BinarySearchStructure<>))
            return Generate(func, props, vector, new BinarySearchStructure<T>(props.DataType, StringComparison.Ordinal));
        if (vector.Type == typeof(ConditionalStructure<>))
            return Generate(func, props, vector, new ConditionalStructure<T>());
        if (vector.Type == typeof(EytzingerSearchStructure<>))
            return Generate(func, props, vector, new EytzingerSearchStructure<T>(props.DataType, StringComparison.Ordinal));
        if (vector.Type == typeof(HashSetChainStructure<>))
            return Generate(func, props, vector, new HashSetChainStructure<T>(HashData.Create(data, props.DataType, 1)));
        if (vector.Type == typeof(HashSetPerfectStructure<>))
            return Generate(func, props, vector, new HashSetPerfectStructure<T>(HashData.Create(data, props.DataType, 1)));
        if (vector.Type == typeof(HashSetLinearStructure<>))
            return Generate(func, props, vector, new HashSetLinearStructure<T>(HashData.Create(data, props.DataType, 1)));
        if (vector.Type == typeof(KeyLengthStructure<>))
            return Generate(func, props, vector, new KeyLengthStructure<T>(props.StringProps!));
        if (vector.Type == typeof(ArrayStructure<>))
            return Generate(func, props, vector, new ArrayStructure<T>());

        throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);
    }

    private static GeneratorSpec Generate<T, TContext>(Func<string, ICodeGenerator> func, DataProperties<T> props, TestVector<T> vector, IStructure<T, TContext> structure) where TContext : IContext<T>
    {
        StructureType structureType = structure switch
        {
            ArrayStructure<T> => StructureType.Array,
            BinarySearchStructure<T> or EytzingerSearchStructure<T> => StructureType.BinarySearch,
            ConditionalStructure<T> => StructureType.Conditional,
            HashSetChainStructure<T> or HashSetPerfectStructure<T> or HashSetLinearStructure<T> => StructureType.HashSet,
            _ => StructureType.Auto
        };

        TContext context = structure.Create(vector.Values);
        ICodeGenerator generator = func(vector.Identifier);

        GeneratorConfig<T> genCfg = new GeneratorConfig<T>(structureType, FastDataGenerator.DefaultStringComparison, props);
        string source = generator.Generate(vector.Values, genCfg, context);
        return new GeneratorSpec(vector.Identifier, source);
    }
}