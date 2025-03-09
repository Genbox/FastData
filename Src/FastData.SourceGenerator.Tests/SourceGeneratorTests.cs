using Genbox.FastData.InternalShared;

namespace Genbox.FastData.SourceGenerator.Tests;

public class SourceGeneratorTests
{
    [Fact]
    public void GenericTest()
    {
        const string src = """
                           using Genbox.FastData;

                           [assembly: FastData<string>("StaticData", ["item1", "item2", "item3"])]
                           """;

        string source = CodeGenerator.RunSourceGenerator<Internal.FastDataSourceGenerator>(src, false, out var compilerDiagnostics, out var codeGenDiagnostics);
        Assert.NotEmpty(source);
        Assert.Empty(compilerDiagnostics);
        Assert.Empty(codeGenDiagnostics);

        //Compile the output to code and test
        Func<string, bool> func = CodeGenerator.GetDelegate<Func<string, bool>>(source, false, true);
        Assert.True(func("item1"));
        Assert.False(func("dontexist"));
    }
}