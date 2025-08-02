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
    [ClassData(typeof(ValueTestVectorTheoryData))]
    public async Task ObjectSupportTest<TKey, TValue>(TestVector<TKey, TValue> vector) where TValue : notnull
    {
        GeneratorSpec spec = Generate(id => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id)), vector);

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
}