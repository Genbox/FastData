using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generator.Rust.Shared;
using Genbox.FastData.Generators;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.Tests;

public class VectorTests(VectorTests.RustContext context) : IClassFixture<VectorTests.RustContext>
{
    [Theory]
    [ClassData(typeof(TestVectorTheoryData))]
    public async Task Test<T>(TestVector<T> data)
    {
        GeneratorSpec spec = Generate(id => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id)), data);
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Verify")
              .DisableDiff();

        RustLanguageDef langDef = new RustLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, spec.Flags.HasFlag(GeneratorFlags.AllAreASCII) ? GeneratorEncoding.ASCII : langDef.Encoding);

        string executable = context.Compiler.Compile(spec.Identifier,
            $$"""
              #![allow(non_camel_case_types)]
              {{spec.Source}}

              fn main() {
              {{FormatList(data.Values, x => $$"""
                                               if !{{spec.Identifier}}::contains({{map.ToValueLabel(x)}}) {
                                                   std::process::exit(0);
                                               }
                                               """, "\n")}}

                  std::process::exit(1);
              }
              """);

        Assert.Equal(1, RunProcess(executable));
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    [SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
    public sealed class RustContext
    {
        public RustCompiler Compiler { get; } = new RustCompiler(false, Path.Combine(Path.GetTempPath(), "FastData", "Rust"));
    }
}