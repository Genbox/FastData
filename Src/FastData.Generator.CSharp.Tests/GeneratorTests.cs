using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.InternalShared;
using Microsoft.CodeAnalysis.CSharp;
using TestHelper = Genbox.FastData.Generator.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class GeneratorTests
{
    private readonly CSharpCodeGenerator _generator;

    public GeneratorTests()
    {
        CSharpGeneratorConfig cfg = new CSharpGeneratorConfig("MyData");
        _generator = new CSharpCodeGenerator(cfg);
    }

    [Theory]
    [MemberData(nameof(GetStructureTypes))]
    internal async Task GenerateStructureType(StructureType structureType, object[] data)
    {
        if (!TestHelper.TryGenerate(_ => _generator, structureType, data, out GeneratorSpec spec))
            return;

        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Verify")
              .DisableDiff();

        if (spec.DataType == DataType.String)
        {
            Func<string, bool> contains = CompilationHelper.GetDelegate<Func<string, bool>>(spec.Source, false);

            foreach (string str in data)
                Assert.True(contains(str));

            Assert.False(contains("dontexist"));
            Assert.False(contains("item11"));
        }
        else if (spec.DataType == DataType.Int32)
        {
            Func<int, bool> contains = CompilationHelper.GetDelegate<Func<int, bool>>(spec.Source, false);

            foreach (int str in data)
                Assert.True(contains(str));

            Assert.False(contains(100));
        }
        else
        {
            //Others we just compile and check for errors
            CSharpCompilation compilation = CompilationHelper.CreateCompilation(spec.Source, false);
            Assert.Empty(compilation.GetDiagnostics());
        }
    }

    public static TheoryData<StructureType, object[]> GetStructureTypes()
    {
        TheoryData<StructureType, object[]> res = new TheoryData<StructureType, object[]>();

        foreach ((StructureType type, object[] data) in TestHelper.GetTestData())
            res.Add(type, data);

        return res;
    }
}