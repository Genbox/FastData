using Genbox.FastData.InternalShared;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class VectorTests
{
    [Theory]
    [ClassData(typeof(TestVectorClass))]
    public void Test(ITestData testData)
    {
        Assert.True(TestVectorHelper.TryGenerate(id => new CSharpCodeGenerator(new CSharpCodeGeneratorConfig(id)), testData, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        string wrapper = $$"""
                           {{spec.Source}}

                           public static class Wrapper
                           {
                               public static bool Contains()
                               {
                           {{FormatList(testData.Items, x => $"""
                                                    if (!{spec.Identifier}.Contains({ToValueLabel(x)}))
                                                        return false;
                                                    """, "\n")}};

                                   return true;
                               }
                           }
                           """;

        Func<bool> contains = CompilationHelper.GetDelegate<Func<bool>>(wrapper, types => types.First(x => x.Name == "Wrapper"), false);
        Assert.True(contains());
    }
}