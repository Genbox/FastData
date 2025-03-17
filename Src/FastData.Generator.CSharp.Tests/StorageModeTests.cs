using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.CSharp.Tests;

/// <summary>
/// These tests are designed to ensure that every supported storage mode can be generated. They function as end-to-end tests that mimics the
/// way users will use the source generator.
/// </summary>
public class StorageModeTests
{
    [Theory, MemberData(nameof(GetStorageModes))]
    public void GenerateStorageMode(StorageMode mode, object[] data)
    {
        FastDataConfig config = new FastDataConfig("MyData", data);
        config.StorageMode = mode;

        string source = FastDataGenerator.Generate(config, new CSharpCodeGenerator(new CSharpGeneratorConfig()));
        File.WriteAllText($@"..\..\..\Generated\StorageModes\{mode}-{config.GetDataType()}.output", source);
    }

    public static TheoryData<StorageMode, object[]> GetStorageModes()
    {
        TheoryData<StorageMode, object[]> data = new TheoryData<StorageMode, object[]>();

        // C# attributes are limited constants of the types included in .NET runtime.
        // For details, see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes#2224-attribute-parameter-types

        foreach (StorageMode mode in Enum.GetValues<StorageMode>())
        {
            data.Add(mode, [true, false]);
            data.Add(mode, [(sbyte)-1, (sbyte)0, (sbyte)1]);
            data.Add(mode, [(byte)0, (byte)1, (byte)2]);
            data.Add(mode, ['a', 'b', 'c']);
            data.Add(mode, [0.0, 1.0, 2.0]);
            data.Add(mode, [0f, 1f, 2f]);
            data.Add(mode, [(short)-1, (short)0, (short)1]);
            data.Add(mode, [(ushort)0, (ushort)1, (ushort)2]);
            data.Add(mode, [-1, 0, 1]);
            data.Add(mode, [0U, 1U, 2U]);
            data.Add(mode, [-1L, 0L, 1L]);
            data.Add(mode, [0UL, 1UL, 2UL]);
            data.Add(mode, ["1", "2", "3", "4", "5"]);
        }

        return data;
    }
}