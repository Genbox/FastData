using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public class FeatureTests(CPlusPlusContext context) : IClassFixture<CPlusPlusContext>
{
    [Theory]
    [ClassData(typeof(ValueTestVectorTheoryData))]
    public async Task ObjectSupportTest<TKey, TValue>(TestVector<TKey, TValue> vector) where TValue : notnull
    {
        GeneratorSpec spec = Generate(id => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)), vector);

        string id = $"{nameof(ObjectSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          #include <string>
                          #include <iostream>

                          {{spec.Source}}

                          int main()
                          {
                              const {{map.GetTypeName(vector.Values[0].GetType())}}* res;
                          {{FormatList(vector.Keys, x => $"""
                                                              if (!{spec.Identifier}::try_lookup({map.ToValueLabel(x)}, res))
                                                                  return 0;
                                                          """, "\n")}}

                              return 1;
                          }
                          """;

        string executable = context.Compiler.Compile(id, source);
        Assert.Equal(1, RunProcess(executable));
    }

    [Theory]
    [ClassData(typeof(FloatNaNZeroTestVectorTheoryData))]
    public async Task FloatNaNOrZeroHashSupportTest<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(id => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)), vector);

        string id = $"{nameof(FloatNaNOrZeroHashSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          #include <string>
                          #include <iostream>

                          {{spec.Source}}

                          int main()
                          {
                          {{FormatList(vector.Keys, x => $"""
                                                              if (!{spec.Identifier}::contains({map.ToValueLabel(x)}))
                                                                  return 0;
                                                          """, "\n")}}

                              return 1;
                          }
                          """;

        string executable = context.Compiler.Compile(id, source);
        Assert.Equal(1, RunProcess(executable));
    }
}