using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Rust.Shared;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.Generator.Rust.Internal.Helpers.CodeHelper;
using static Genbox.FastData.InternalShared.TestHelper;

namespace Genbox.FastData.Generator.Rust.Tests;

public class VectorTests(VectorTests.RustContext context) : IClassFixture<VectorTests.RustContext>
{
    [Theory]
    [ClassData(typeof(TestVectorClass))]
    public void Test(StructureType type, object[] data)
    {
        Assert.True(TestVectorHelper.TryGenerate(id => new RustCodeGenerator(new RustCodeGeneratorConfig(id)), type, data, out GeneratorSpec spec));

        string executable = context.Compiler.Compile(spec.Identifier,
            $$"""
              #![allow(non_camel_case_types)]
              {{spec.Source}}

              fn main() {
              {{FormatList(data, x => $$"""
                                        if !{{spec.Identifier}}::contains({{ToValueLabel(x)}}) {
                                            std::process::exit(0);
                                        }
                                        """, "\n")}}

                  std::process::exit(1);
              }
              """);

        Assert.Equal(1, RunProcess(executable));
    }

    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public sealed class RustContext
    {
        public RustCompiler Compiler { get; } = new RustCompiler(false, Path.Combine(Path.GetTempPath(), "FastData", "Rust"));
    }
}