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
    }

    public static TheoryData<StructureType, object[]> GetStructureTypes()
    {
        TheoryData<StructureType, object[]> res = new TheoryData<StructureType, object[]>();

        foreach ((StructureType type, object[] data) in TestHelper.GetTestData())
            res.Add(type, data);

        return res;
    }
}