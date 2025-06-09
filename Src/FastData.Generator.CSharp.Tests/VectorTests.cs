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
    public async Task Test<T>(TestVector<T> data)
    {
        GeneratorSpec spec = Generate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), data);
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Verify")
              .DisableDiff();

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