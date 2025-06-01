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
                         using Genbox.FastData.SourceGenerator;
                         using Genbox.FastData.Enums;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], StructureType = StructureType.{ds})]
                         """;

        string output = RunGenerator(source);
        await Verify(output)
              .UseFileName(ds.ToString())
              .UseDirectory("Verify")
              .DisableDiff();
    }

    [Fact]
    public async Task NamespaceTest()
    {
        const string source = """
                              using Genbox.FastData.SourceGenerator;

                              [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], Namespace = "MyNamespace")]
                              """;

        string output = RunGenerator(source);
        await Verify(output)
              .UseFileName("namespace")
              .UseDirectory("Verify")
              .DisableDiff();
    }

    [Theory]
    [InlineData(ClassVisibility.Internal)]
    [InlineData(ClassVisibility.Public)]
    public async Task ClassVisibilityTest(ClassVisibility visibility)
    {
        string source = $"""
                         using Genbox.FastData.SourceGenerator;
                         using Genbox.FastData.Generator.CSharp.Enums;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], ClassVisibility = ClassVisibility.{visibility})]
                         """;

        string output = RunGenerator(source);

        await Verify(output)
              .UseFileName(visibility.ToString())
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
                         using Genbox.FastData.SourceGenerator;
                         using Genbox.FastData.Generator.CSharp.Enums;

                         [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"], ClassType = ClassType.{type})]
                         """;

        string output = RunGenerator(source);

        await Verify(output)
              .UseFileName(type.ToString())
              .UseDirectory("Verify")
              .DisableDiff();
    }

    //TODO: Test StorageOptions

    private static string RunGenerator(string source)
    {
        string output = SourceGenHelper.RunSourceGenerator<Internal.FastDataSourceGenerator>(source, false, out var compilerDiagnostics, out var codeGenDiagnostics);
        Assert.Empty(compilerDiagnostics);
        Assert.Empty(codeGenDiagnostics);
        Assert.NotEmpty(output); //We test the source later to show diagnostics first

        return output;
    }
}