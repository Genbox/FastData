using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class GeneratorTests
{
    private readonly CPlusPlusCodeGenerator _generator = new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig());

    [Theory]
    [MemberData(nameof(GetDataStructures))]
    internal void GenerateDataStructure(StructureType structureType, object[] data)
    {
        if (!TestHelper.TryGenerateDataStructure(_generator, structureType, data, out GeneratorSpec spec))
            return;

        Assert.NotEmpty(spec.Source);

        File.WriteAllText($@"..\..\..\Generated\{spec.Identifier}.output", spec.Source);
    }

    public static TheoryData<StructureType, object[]> GetDataStructures()
    {
        TheoryData<StructureType, object[]> res = new TheoryData<StructureType, object[]>();

        foreach (StructureType structure in Enum.GetValues<StructureType>().Where(x => x != StructureType.Auto))
            foreach (object[] data in TestHelper.GetAllSets())
                res.Add(structure, data);

        return res;
    }
}