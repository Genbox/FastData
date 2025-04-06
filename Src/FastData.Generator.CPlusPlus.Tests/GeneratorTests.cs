using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class GeneratorTests
{
    private readonly CPlusPlusCodeGenerator _generator = new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig());

    [Theory]
    [MemberData(nameof(GetDataStructures))]
    internal void GenerateDataStructure(DataStructure dataStructure, object[] data)
    {
        if (!TestHelper.TryGenerateDataStructure(_generator, dataStructure, data, out GeneratorSpec spec))
            return;

        Assert.NotEmpty(spec.Source);

        File.WriteAllText($@"..\..\..\Generated\{spec.Identifier}.output", spec.Source);
    }

    public static TheoryData<DataStructure, object[]> GetDataStructures()
    {
        TheoryData<DataStructure, object[]> res = new TheoryData<DataStructure, object[]>();

        foreach (DataStructure structure in Enum.GetValues<DataStructure>().Where(x => x != DataStructure.Auto))
            foreach (object[] data in TestHelper.GetAllSets())
                res.Add(structure, data);

        return res;
    }
}