using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class VectorTests
{
    [Theory]
    [ClassData(typeof(TestVectorTheoryData))]
    public void Test<T>(TestVector<T> data)
    {
        Assert.True(TryGenerate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        CSharpLanguageDef langDef = new CSharpLanguageDef();
        TypeHelper helper = new TypeHelper(new TypeMap(langDef.TypeDefinitions));

        string wrapper = $$"""
                           {{spec.Source}}

                           public static class Wrapper
                           {
                               public static bool Contains()
                               {
                           {{FormatList(data.Values, x => $"""
                                                           if (!{spec.Identifier}.Contains({helper.ToValueLabel(x)}))
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