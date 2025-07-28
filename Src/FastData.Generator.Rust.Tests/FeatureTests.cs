using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.Tests;

public class FeatureTests(RustContext context) : IClassFixture<RustContext>
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

        GeneratorSpec spec = Generate(id => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id)), vector, values);

        string id = $"{nameof(ObjectSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        RustLanguageDef langDef = new RustLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          #![allow(non_camel_case_types)]
                          {{spec.Source}}

                          fn main() {
                          {{FormatList(vector.Keys, x => $$"""
                                                               if ({{spec.Identifier}}::try_lookup({{map.ToValueLabel(x)}}).is_none()) {
                                                                      std::process::exit(0);
                                                                  }
                                                           """, "\n")}}

                              std::process::exit(1);
                          }
                          """;

        string executable = context.Compiler.Compile(id, source);
        Assert.Equal(1, RunProcess(executable));
    }

    [Theory]
    [ClassData(typeof(FloatNaNZeroTestVectorTheoryData))]
    public async Task FloatNaNOrZeroHashSupportTest<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(id => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id)), vector);

        string id = $"{nameof(FloatNaNOrZeroHashSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        RustLanguageDef langDef = new RustLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          #![allow(non_camel_case_types)]
                          {{spec.Source}}

                          fn main() {
                          {{FormatList(vector.Keys, x => $$"""
                                                               if (!{{spec.Identifier}}::contains({{map.ToValueLabel(x)}})) {
                                                                      std::process::exit(0);
                                                                  }
                                                           """, "\n")}}

                              std::process::exit(1);
                          }
                          """;

        string executable = context.Compiler.Compile(id, source);
        Assert.Equal(1, RunProcess(executable));
    }

    internal class Person
    {
        public required int Age { get; set; }
        public required string Name { get; set; }
        public Person? Other { get; set; }
    }
}