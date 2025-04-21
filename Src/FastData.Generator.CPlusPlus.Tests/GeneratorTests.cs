using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class GeneratorTests
{
    private readonly CPlusPlusCodeGenerator _generator = new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig("my_data"));

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