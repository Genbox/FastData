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

public class FeatureTests(CPlusPlusContext context) : IClassFixture<CPlusPlusContext>
{
    [Theory]
    [ClassData(typeof(SimpleTestVectorTheoryData))]
    public async Task ObjectSupportTest<T>(TestVector<T> vector)
    {
        Person[] values =
        [
            new Person { Age = 1, Name = "Bob", Other = new Person { Name = "Anna", Age = 4 } },
            new Person { Age = 2, Name = "Billy" },
            new Person { Age = 3, Name = "Bibi" },
        ];

        GeneratorSpec spec = Generate(id => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)), vector, values);

        string id = $"{nameof(ObjectSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string testSource = $$"""
                              #include <string>
                              #include <iostream>

                              {{spec.Source}}

                              int main()
                              {
                                  const Person* res;
                              {{FormatList(vector.Keys, x => $"""
                                                                  if (!{spec.Identifier}::try_lookup({map.ToValueLabel(x)}, res))
                                                                      return 0;
                                                              """, "\n")}}

                                  return 1;
                              }
                              """;

        string executable = context.Compiler.Compile(id, testSource);
        Assert.Equal(1, RunProcess(executable));
    }

    internal class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public Person Other { get; set; }
    }
}