using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Abstracts;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Shared;
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
        cfg.ClassType = ClassType.Instance;
        _generator = new CSharpCodeGenerator(cfg);
    }

    [Theory]
    [MemberData(nameof(GetDataStructures))]
    internal void GenerateDataStructure(StructureType structureType, object[] data)
    {
        if (!TestHelper.TryGenerateDataStructure(_generator, structureType, data, out GeneratorSpec spec))
            return;

        Assert.NotEmpty(spec.Source);

        File.WriteAllText($@"..\..\..\Generated\{spec.Identifier}.output", spec.Source);

        if (spec.DataType == DataType.String)
        {
            IFastSet<string> set = CodeGenerator.CreateFastSet<string>(spec.Source, false);

            foreach (string str in data)
                Assert.True(set.Contains(str));

            Assert.False(set.Contains("dontexist"));
            Assert.False(set.Contains("item11"));
        }
        else if (spec.DataType == DataType.Int32)
        {
            IFastSet<int> set = CodeGenerator.CreateFastSet<int>(spec.Source, false);

            foreach (int str in data)
                Assert.True(set.Contains(str));

            Assert.False(set.Contains(100));
        }
        else
        {
            //Others we just compile and check for errors
            CSharpCompilation compilation = CompilationHelper.CreateCompilation(spec.Source, false);
            Assert.Empty(compilation.GetDiagnostics());
        }
    }

    public static TheoryData<StructureType, object[]> GetDataStructures()
    {
        TheoryData<StructureType, object[]> res = new TheoryData<StructureType, object[]>();

        foreach ((StructureType type, object[] data) in TestHelper.GetTestData())
            res.Add(type, data);

        return res;
    }
}