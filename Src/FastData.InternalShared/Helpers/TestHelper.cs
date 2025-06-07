using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
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

    /// <summary>This variant of TryGenerate calls the public API of FastDataGenerator such that we test it like a user would invoke it.</summary>
    public static bool TryGenerate<T>(Func<string, ICodeGenerator> gen, TestData<T> testData, out GeneratorSpec spec)
    {
        if (FastDataGenerator.TryGenerate(testData.Values, new FastDataConfig(testData.StructureType), gen(testData.Identifier), out string? source))
        {
            spec = new GeneratorSpec(testData.Identifier, source!);
            return true;
        }

        spec = default;
        return false;
    }

    /// <summary>This variant of TryGenerate bypasses the public API to test more advanced combinations of parameters</summary>
    public static bool TryGenerate<T>(Func<string, ICodeGenerator> gen, TestVector<T> vector, out GeneratorSpec spec)
    {
        DataProperties<T> props = DataProperties<T>.Create(vector.Values);

        IStructure<T>? structure;
        T[] data = vector.Values;

        if (vector.Type == typeof(SingleValueStructure<>))
            structure = new SingleValueStructure<T>();
        else if (vector.Type == typeof(BinarySearchStructure<>))
            structure = new BinarySearchStructure<T>(props.DataType, StringComparison.Ordinal);
        else if (vector.Type == typeof(ConditionalStructure<>))
            structure = new ConditionalStructure<T>();
        else if (vector.Type == typeof(EytzingerSearchStructure<>))
            structure = new EytzingerSearchStructure<T>(props.DataType, StringComparison.Ordinal);
        else if (vector.Type == typeof(HashSetChainStructure<>))
            structure = new HashSetChainStructure<T>(HashData.Create(data, props.DataType, 1));
        else if (vector.Type == typeof(HashSetPerfectStructure<>))
            structure = new HashSetPerfectStructure<T>(HashData.Create(data, props.DataType, 1));
        else if (vector.Type == typeof(HashSetLinearStructure<>))
            structure = new HashSetLinearStructure<T>(HashData.Create(data, props.DataType, 1));
        else if (vector.Type == typeof(KeyLengthStructure<>))
            structure = new KeyLengthStructure<T>(props.StringProps!);
        else if (vector.Type == typeof(ArrayStructure<>))
            structure = new ArrayStructure<T>();
        else
            throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);

        StructureType structureType = structure switch
        {
            ArrayStructure<T> => StructureType.Array,
            BinarySearchStructure<T> or EytzingerSearchStructure<T> => StructureType.BinarySearch,
            ConditionalStructure<T> => StructureType.Conditional,
            HashSetChainStructure<T> or HashSetPerfectStructure<T> or HashSetLinearStructure<T> => StructureType.HashSet,
            _ => StructureType.Auto
        };

        if (!structure.TryCreate(vector.Values, out IContext? context))
        {
            spec = default;
            return false;
        }

        ICodeGenerator generator = gen(vector.Identifier);
        GeneratorConfig<T> genCfg = new GeneratorConfig<T>(structureType, FastDataGenerator.DefaultStringComparison, props, null);
        if (generator.TryGenerate(genCfg, context, out string? source))
        {
            spec = new GeneratorSpec(vector.Identifier, source);
            return true;
        }

        spec = default;
        return false;
    }
}