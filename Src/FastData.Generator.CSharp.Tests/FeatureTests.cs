using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class FeatureTests
{
    [Theory]
    [ClassData(typeof(ValueTestVectorTheoryData))]
    public async Task ObjectSupportTest<TKey, TValue>(TestVector<TKey, TValue> vector) where TValue : notnull
    {
        GeneratorSpec spec = Generate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), vector);

        string id = $"{nameof(ObjectSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CSharpLanguageDef langDef = new CSharpLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          {{spec.Source}}

                          public static class Wrapper
                          {
                              public static bool Run()
                              {
                          {{FormatList(vector.Keys, x => $"""
                                                              if (!{spec.Identifier}.TryLookup({map.ToValueLabel(x)}, out _))
                                                                  return false;
                                                          """, "\n")}}

                                  return true;
                              }
                          }
                          """;

        Func<bool> contains = CompilationHelper.GetDelegate<Func<bool>>(source, types => types.First(x => x.Name == "Wrapper"), false);
        Assert.True(contains());
    }

    [Theory]
    [ClassData(typeof(FloatNaNZeroTestVectorTheoryData))]
    public async Task FloatNaNOrZeroHashSupportTest<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), vector);

        string id = $"{nameof(FloatNaNOrZeroHashSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CSharpLanguageDef langDef = new CSharpLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          {{spec.Source}}

                          public static class Wrapper
                          {
                              public static bool Run()
                              {
                          {{FormatList(vector.Keys, x => $"""
                                                              if (!{spec.Identifier}.Contains({map.ToValueLabel(x)}))
                                                                  return false;
                                                          """, "\n")}}

                                  return true;
                              }
                          }
                          """;

        Func<bool> contains = CompilationHelper.GetDelegate<Func<bool>>(source, types => types.First(x => x.Name == "Wrapper"), false);
        Assert.True(contains());
    }
}