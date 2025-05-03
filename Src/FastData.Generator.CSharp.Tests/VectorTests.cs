using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Helpers;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class VectorTests
{
    [Theory]
    [ClassData(typeof(TestVectorClass))]
    public void Test(StructureType type, object[] data)
    {
        Assert.True(TestVectorHelper.TryGenerate(id => new CSharpCodeGenerator(new CSharpCodeGeneratorConfig(id)), type, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        string wrapper = $$"""
                           {{spec.Source}}

                           public static class Wrapper
                           {
                               public static bool Contains() => {{spec.Identifier}}.Contains({{CodeHelper.ToValueLabel(data[0])}});
                           }
                           """;

        Func<bool> contains = CompilationHelper.GetDelegate<Func<bool>>(wrapper, types => types.First(x => x.Name == "Wrapper"), false);
        Assert.True(contains());
    }
}