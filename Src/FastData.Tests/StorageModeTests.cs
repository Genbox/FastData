using Genbox.FastData.Enums;
using Genbox.FastData.Internal;
using Genbox.FastData.InternalShared;
using Microsoft.CodeAnalysis;

namespace Genbox.FastData.Tests;

/// <summary>
/// These tests are designed to ensure that every supported storage mode can be generated. They function as end-to-end tests that mimics the
/// way users will use the source generator.
/// </summary>
public class StorageModeTests
{
    [Theory, MemberData(nameof(GetStorageModes))]
    public void GenerateStorageMode(StorageMode mode, string type, string data)
    {
        string source = $"""
                         using Genbox.FastData;
                         using Genbox.FastData.Enums;
                         [assembly: FastData<{type}>("ImmutableSet", [ {data} ], StorageMode = StorageMode.{mode})]
                         """;

        string generated = GetGeneratedOutput(source);

        File.WriteAllText($@"..\..\..\Generated\StorageModes\{mode}-{type}.output", generated);
    }

    private static string GetGeneratedOutput(string source)
    {
        const string headers = """
                               using Genbox.FastData;
                               using Genbox.FastData.Enums;

                               """;

        source = headers + source;
        string result = CodeGenerator.RunSourceGenerator<FastDataGenerator>(source, false, out Diagnostic[] compilerDiag, out Diagnostic[] codeGenDiag);
        Assert.Empty(compilerDiag);
        Assert.Empty(codeGenDiag);
        Assert.NotEmpty(result);

        return result;
    }

    public static TheoryData<StorageMode, string, string> GetStorageModes()
    {
        TheoryData<StorageMode, string, string> data = new TheoryData<StorageMode, string, string>();

        // C# attributes are limited constants of the types included in .NET runtime.
        // For details, see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes#2224-attribute-parameter-types

        foreach (StorageMode mode in Enum.GetValues<StorageMode>())
        {
            data.Add(mode, "bool", "true, false");
            data.Add(mode, "sbyte", "-1, 0, 1");
            data.Add(mode, "byte", "0, 1, 2");
            data.Add(mode, "char", "'a', 'b', 'c'");
            data.Add(mode, "double", "0.0, 1.0, 2.0");
            data.Add(mode, "float", "0f, 1f, 2f");
            data.Add(mode, "short", "-1, 0, 1");
            data.Add(mode, "ushort", "0, 1, 2");
            data.Add(mode, "int", "-1, 0, 1");
            data.Add(mode, "uint", "0, 1, 2");
            data.Add(mode, "long", "-1, 0, 1");
            data.Add(mode, "ulong", "0, 1, 2");

            data.Add(mode, "string", """
                                     "1", "2", "3", "4", "5"
                                     """);
        }

        return data;
    }
}