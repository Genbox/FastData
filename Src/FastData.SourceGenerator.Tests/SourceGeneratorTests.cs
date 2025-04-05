using Genbox.FastData.InternalShared;

namespace Genbox.FastData.SourceGenerator.Tests;

public class SourceGeneratorTests
{
    // C# attributes are limited constants of the types included in .NET runtime.
    // For details, see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes#2224-attribute-parameter-types

    [Fact]
    public void GenericTest()
    {
        const string src = """
                           using Genbox.FastData.SourceGenerator;

                           [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"])]
                           """;

        string source = SourceGenHelper.RunSourceGenerator<Internal.FastDataSourceGenerator>(src, false, out var compilerDiagnostics, out var codeGenDiagnostics);
        Assert.NotEmpty(source);
        Assert.Empty(compilerDiagnostics);
        Assert.Empty(codeGenDiagnostics);

        //Compile the output to code and test
        Func<string, bool> func = CompilationHelper.GetDelegate<Func<string, bool>>(source, false, true);
        Assert.True(func("item1"));
        Assert.False(func("dontexist"));
    }
}