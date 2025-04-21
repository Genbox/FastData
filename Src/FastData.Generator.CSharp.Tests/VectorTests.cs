using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Helpers;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.InternalShared;
using TestHelper = Genbox.FastData.Generator.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class VectorTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void Test(StructureType type, object[] data)
    {
        Assert.True(TestHelper.TryGenerate(id => new CSharpCodeGenerator(new CSharpGeneratorConfig(id)), type, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        string wrapper = $$"""
                           {{spec.Source}}

                           public static class Wrapper
                           {
                               public static bool Contains() => {{spec.Identifier}}.Contains({{CodeHelper.ToValueLabel(data[0])}});
                           }
                           """;

        Func<bool> contains = CompilationHelper.GetDelegate<Func<bool>>(wrapper, types => types.First(x=> x.Name == "Wrapper"), false);
        Assert.True(contains());
    }

    public static TheoryData<StructureType, object[]> GetTestData()
    {
        TheoryData<StructureType, object[]> res = new TheoryData<StructureType, object[]>();

        foreach ((StructureType type, object[] data) in TestHelper.GetTestVectors())
            res.Add(type, data);

        return res;
    }
}