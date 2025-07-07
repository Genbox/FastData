using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.SourceGenerator.Attributes;

namespace Genbox.FastData.SourceGenerator.Tests;

public class SourceGeneratorTests
{
    // C# attributes are limited constants of the types included in .NET runtime.
    // For details, see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes#2224-attribute-parameter-types

    [Fact]
    public void GenericTest()
    {
        const string source = """
                              using Genbox.FastData.SourceGenerator.Attributes;

                              [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"])]
                              """;

        string output = RunGenerator(source);
        Assert.Contains("StaticData", output, StringComparison.Ordinal); //It must contain the name we gave it

        //Compile the output to code and test
        Func<string, bool> func = CompilationHelper.GetDelegate<Func<string, bool>>(output, false);
        Assert.True(func("item1")); //It must return true for the 3 items we gave
        Assert.True(func("item2"));
        Assert.True(func("item3"));
        Assert.False(func("dontexist"));
    }

    [Theory]
    [InlineData(StructureType.Array)]
    [InlineData(StructureType.Conditional)]
    [InlineData(StructureType.BinarySearch)]
    [InlineData(StructureType.HashSet)]
    public async Task DataStructureTest(StructureType ds)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator.Attributes;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], StructureType = StructureType.{ds}, AnalysisLevel = AnalysisLevel.Disabled)]
                         """;

        string output = RunGenerator(source);
        await Verify(output)
              .UseFileName($"{nameof(DataStructureTest)}-{ds}")
              .UseDirectory("Verify")
              .DisableDiff();
    }

    [Fact]
    public async Task NamespaceTest()
    {
        const string source = """
                              using Genbox.FastData.SourceGenerator.Attributes;

                              [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], Namespace = "MyNamespace", AnalysisLevel = AnalysisLevel.Disabled)]
                              """;

        string output = RunGenerator(source);
        await Verify(output)
              .UseFileName(nameof(NamespaceTest))
              .UseDirectory("Verify")
              .DisableDiff();
    }

    [Theory]
    [InlineData(ClassVisibility.Internal)]
    [InlineData(ClassVisibility.Public)]
    public async Task ClassVisibilityTest(ClassVisibility visibility)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator.Attributes;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], ClassVisibility = ClassVisibility.{visibility}, AnalysisLevel = AnalysisLevel.Disabled)]
                         """;

        string output = RunGenerator(source);

        await Verify(output)
              .UseFileName($"{nameof(ClassVisibilityTest)}-{visibility}")
              .UseDirectory("Verify")
              .DisableDiff();
    }

    [Theory]
    [InlineData(ClassType.Static)]
    [InlineData(ClassType.Instance)]
    [InlineData(ClassType.Struct)]
    public async Task ClassTypeTest(ClassType type)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator.Attributes;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], ClassType = ClassType.{type}, AnalysisLevel = AnalysisLevel.Disabled)]
                         """;

        string output = RunGenerator(source);

        await Verify(output)
              .UseFileName($"{nameof(ClassTypeTest)}-{type}")
              .UseDirectory("Verify")
              .DisableDiff();
    }

    [Theory]
    [InlineData(AnalysisLevel.Disabled)]
    [InlineData(AnalysisLevel.Fast)]
    [InlineData(AnalysisLevel.Balanced)]
    [InlineData(AnalysisLevel.Aggressive)]
    public void AnalysisLevelTest(AnalysisLevel al)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator.Attributes;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], StructureType = StructureType.HashSet, AnalysisLevel = AnalysisLevel.{al})]
                         """;

        string output = RunGenerator(source);

        //We can't verify the outputs as they depend on the computer configuration and will be different across executions. Instead, we just assert it is not empty.
        Assert.NotEmpty(output);
    }

    private static string RunGenerator(string source)
    {
        string output = SourceGenHelper.RunSourceGenerator<FastDataSourceGenerator>(source, false, out var compilerDiagnostics, out var codeGenDiagnostics);
        Assert.Empty(compilerDiagnostics);
        Assert.Empty(codeGenDiagnostics);
        Assert.NotEmpty(output); //We test the source later to show diagnostics first

        return output;
    }
}