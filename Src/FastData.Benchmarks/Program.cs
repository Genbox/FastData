using BenchmarkDotNet.Running;
using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.Benchmarks;

internal static class Program
{
    private static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

    public static void GenerateFiles()
    {
        // This code is used to generate the files in the Generated folder.

        int[] sizes = [1, 5, 10, 50, 100, 500, 1000];
        StructureType[] structures = [StructureType.Array, StructureType.Conditional, StructureType.BinarySearch, StructureType.ConstMap, StructureType.HashTable];

        foreach (StructureType type in structures)
        {
            foreach (int size in sizes)
                DoStructure(type, size);
        }
    }

    private static void DoStructure(StructureType type, int size)
    {
        Random rng = new Random(42);
        string[] data = Enumerable.Range(0, size).Select(_ => TestHelper.GenerateRandomString(rng, rng.Next(5, 10))).ToArray();

        CSharpCodeGenerator generator = new CSharpCodeGenerator(new CSharpCodeGeneratorConfig($"{type}Structure_String_{size}"));

        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = type;

        StringGenerationResult result = FastDataGenerator.Generate(data, config, generator);
        File.WriteAllText($"Gen-{type}Structure-{size}.cs", result.Source);
    }
}