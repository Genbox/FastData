using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Shared;
using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class GeneratorTests
{
    private readonly CSharpCodeGenerator _generator;

    public GeneratorTests()
    {
        CSharpGeneratorConfig cfg = new CSharpGeneratorConfig();
        cfg.ClassType = ClassType.Instance;
        _generator = new CSharpCodeGenerator(cfg);
    }

    [Theory]
    [MemberData(nameof(GetDataStructures))]
    internal void GenerateDataStructure(DataStructure dataStructure, object[] data)
    {
        if (!TestHelper.TryGenerateDataStructure(_generator, dataStructure, data, out GeneratorSpec spec))
            return;

        Assert.NotEmpty(spec.Source);

        File.WriteAllText($@"..\..\..\Generated\{spec.Identifier}.output", spec.Source);

        if (spec.DataType == KnownDataType.String)
        {
            IFastSet<string> set = CodeGenerator.CreateFastSet<string>(spec.Source, false);

            foreach (string str in data)
                Assert.True(set.Contains(str));

            Assert.False(set.Contains("dontexist"));
            Assert.False(set.Contains("item11"));
        }
        else if (spec.DataType == KnownDataType.Int32)
        {
            IFastSet<int> set = CodeGenerator.CreateFastSet<int>(spec.Source, false);

            foreach (int str in data)
                Assert.True(set.Contains(str));

            Assert.False(set.Contains(100));
        }
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