using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.SourceGenerator.Tests;

public class SourceGeneratorTests
{
    // C# attributes are limited constants of the types included in .NET runtime.
    // For details, see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes#2224-attribute-parameter-types

    [Fact]
    public void GenericTest()
    {
        const string source = """
                              using Genbox.FastData.SourceGenerator;

                              [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"])]
                              """;

        string output = RunGenerator(source);
        Assert.Contains("StaticData", output, StringComparison.Ordinal); //It must contain the name we gave it

        //Compile the output to code and test
        Func<string, bool> func = CompilationHelper.GetDelegate<Func<string, bool>>(output, false, true);
        Assert.True(func("item1")); //It must return true for the 3 items we gave
        Assert.True(func("item2"));
        Assert.True(func("item3"));
        Assert.False(func("dontexist"));
    }

    [Theory]
    [InlineData(DataStructure.SingleValue)]
    public void DataStructureSingleTest(DataStructure ds)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator;
                         using Genbox.FastData.Enums;

                         [assembly: FastData<string>("StaticData", ["item1"], DataStructure = DataStructure.{ds})]
                         """;

        string output = RunGenerator(source);

        Assert.Contains("Structure: " + ds, output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DataStructure.Array)]
    [InlineData(DataStructure.Conditional)]
    [InlineData(DataStructure.BinarySearch)]
    [InlineData(DataStructure.EytzingerSearch)]
    [InlineData(DataStructure.PerfectHashGPerf)]
    [InlineData(DataStructure.PerfectHashBruteForce)]
    [InlineData(DataStructure.HashSetChain)]
    [InlineData(DataStructure.HashSetLinear)]
    [InlineData(DataStructure.KeyLength)]
    public void DataStructureTest(DataStructure ds)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator;
                         using Genbox.FastData.Enums;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], DataStructure = DataStructure.{ds})]
                         """;

        string output = RunGenerator(source);

        Assert.Contains("Structure: " + ds, output, StringComparison.Ordinal);
    }

    [Fact]
    public void NamespaceTest()
    {
        const string source = """
                              using Genbox.FastData.SourceGenerator;

                              [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], Namespace = "MyNamespace")]
                              """;

        string output = RunGenerator(source);
        Assert.Contains("namespace MyNamespace;", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ClassVisibility.Internal)]
    [InlineData(ClassVisibility.Public)]
    public void ClassVisibilityTest(ClassVisibility visibility)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator;
                         using Genbox.FastData.Generator.CSharp.Enums;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], ClassVisibility = ClassVisibility.{visibility})]
                         """;

        string output = RunGenerator(source);

        switch (visibility)
        {
            case ClassVisibility.Internal:
                Assert.Contains("internal static class StaticData ", output, StringComparison.Ordinal);
                break;
            case ClassVisibility.Public:
                Assert.Contains("public static class StaticData ", output, StringComparison.Ordinal);
                break;
            case ClassVisibility.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
        }
    }

    [Theory]
    [InlineData(ClassType.Static)]
    [InlineData(ClassType.Instance)]
    [InlineData(ClassType.Struct)]
    public void ClassTypeTest(ClassType type)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator;
                         using Genbox.FastData.Generator.CSharp.Enums;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], ClassType = ClassType.{type})]
                         """;

        string output = RunGenerator(source);

        switch (type)
        {
            case ClassType.Static:
                Assert.Contains("static class StaticData", output, StringComparison.Ordinal);
                break;
            case ClassType.Instance:
                Assert.Contains("partial class StaticData", output, StringComparison.Ordinal);
                break;
            case ClassType.Struct:
                Assert.Contains("partial struct StaticData", output, StringComparison.Ordinal);
                break;
            case ClassType.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    //TODO: Test StorageOptions

    private static string RunGenerator(string source)
    {
        string output = SourceGenHelper.RunSourceGenerator<Internal.FastDataSourceGenerator>(source, false, out var compilerDiagnostics, out var codeGenDiagnostics);
        Assert.Empty(compilerDiagnostics);
        Assert.Empty(codeGenDiagnostics);
        Assert.NotEmpty(output); //We test source later in order to show diagnostics first

        return output;
    }
}