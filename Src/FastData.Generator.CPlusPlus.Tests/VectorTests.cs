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

public class VectorTests(CPlusPlusContext context) : IClassFixture<CPlusPlusContext>
{
    [Theory]
    [ClassData(typeof(TestVectorTheoryData))]
    public async Task Test<T>(TestVector<T> data)
    {
        GeneratorSpec spec = Generate(id => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)), data);
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

                          int main(int argc, char* argv[])
                          {
                          {{FormatList(data.Values, x => $"""
                                                          if (!{spec.Identifier}::contains({map.ToValueLabel(x)}))
                                                              return false;
                                                          """, "\n")}}

                              return 1;
                          }
                          """;

        string executable = context.Compiler.Compile(spec.Identifier, source);

        Assert.Equal(1, RunProcess(executable));
    }
}