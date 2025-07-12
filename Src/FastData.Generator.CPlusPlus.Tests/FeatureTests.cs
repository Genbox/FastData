using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class FeatureTests(CPlusPlusContext context) : IClassFixture<CPlusPlusContext>
{
    [Fact]
    public async Task StructSupportTest()
    {
        int[] values = [1, 2, 3];
        const string id = nameof(StructSupportTest);

        string genSource = FastDataGenerator.GenerateKeyed(values, [
            new X { Age = 1, Name = "Bob" },
            new X { Age = 2, Name = "Billy" },
            new X { Age = 3, Name = "Bibi" },
        ], new FastDataConfig { StructureType = StructureType.Array }, CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)));

        await Verify(genSource)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string testSource = $$"""
                              #include <string>
                              #include <iostream>

                              {{genSource}}

                              int main(int argc, char* argv[])
                              {
                              {{FormatList(values, x => $"""
                                                         if (!{id}::contains({map.ToValueLabel(x)}))
                                                             return false;
                                                         """, "\n")}}

                                  return 1;
                              }
                              """;

        string executable = context.Compiler.Compile(id, testSource);
        Assert.Equal(1, RunProcess(executable));
    }

    internal struct X
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}