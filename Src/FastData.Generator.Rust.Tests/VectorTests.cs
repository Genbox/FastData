using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.Tests;

[SuppressMessage("Usage", "xUnit1039:The type argument to theory data is not compatible with the type of the corresponding test method parameter")]
public class VectorTests(RustContext context) : IClassFixture<RustContext>
{
    [Theory]
    [ClassData(typeof(TestVectorTheoryData))]
    public async Task Test<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(id => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id)), vector);
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Vectors")
              .DisableDiff();

        RustLanguageDef langDef = new RustLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, spec.Flags.HasFlag(GeneratorFlags.AllAreASCII) ? GeneratorEncoding.ASCII : langDef.Encoding);

        string executable = context.Compiler.Compile(spec.Identifier,
            $$"""
              #![allow(non_camel_case_types)]
              {{spec.Source}}

              fn main() {
              {{FormatList(vector.Keys, x => $$"""
                                                   if !{{spec.Identifier}}::contains({{map.ToValueLabel(x)}}) {
                                                       std::process::exit(0);
                                               }
                                               """, "\n")}}

              {{FormatList(vector.NotPresent, x => $$"""
                                                         if {{spec.Identifier}}::contains({{map.ToValueLabel(x)}}) {
                                                             std::process::exit(0);
                                                     }
                                                     """, "\n")}}

                  std::process::exit(1);
              }
              """);

        Assert.Equal(1, RunProcess(executable).ExitCode);
    }
}