using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.Rust.Tests;

public class GeneratorTests
{
    private readonly RustCodeGenerator _generator = new RustCodeGenerator(new RustGeneratorConfig("MyData"));

    [Theory]
    [MemberData(nameof(GetStructureTypes))]
    internal void GenerateStructureType(StructureType structureType, object[] data)
    {
        if (!TestHelper.TryGenerate(_generator, structureType, data, out GeneratorSpec spec))
            return;

        Assert.NotEmpty(spec.Source);

        File.WriteAllText($@"..\..\..\Generated\{spec.Identifier}.output", spec.Source);
    }

    public static TheoryData<StructureType, object[]> GetStructureTypes()
    {
        TheoryData<StructureType, object[]> res = new TheoryData<StructureType, object[]>();

        foreach ((StructureType type, object[] data) in TestHelper.GetTestData())
            res.Add(type, data);

        return res;
    }
}