using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public class VectorTests(CPlusPlusContext context) : IClassFixture<CPlusPlusContext>
{
    [Theory]
    [ClassData(typeof(TestVectorTheoryData))]
    public async Task Test<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(id => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)), vector);
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Vectors")
              .DisableDiff();

        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, spec.Flags.HasFlag(GeneratorFlags.AllAreASCII) ? GeneratorEncoding.ASCII : langDef.Encoding);

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

                          {{FormatList(vector.NotPresent, x => $"""
                                                                    if ({spec.Identifier}::contains({map.ToValueLabel(x)}))
                                                                        return 0;
                                                                """, "\n")}}

                              return 1;
                          }
                          """;

        string executable = context.Compiler.Compile(spec.Identifier, source);
        Assert.Equal(1, RunProcess(executable));
    }
}