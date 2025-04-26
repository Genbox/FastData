using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Helpers;
using Genbox.FastData.Generator.CPlusPlus.Shared;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.InternalShared.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class VectorTests(VectorTests.CPlusPlusContext context) : IClassFixture<VectorTests.CPlusPlusContext>
{
    [Theory]
    [ClassData(typeof(TestVectorClass))]
    public void Test(StructureType type, object[] data)
    {
        Assert.True(TestVectorHelper.TryGenerate(id => new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig(id)), type, data, out GeneratorSpec spec));

        string executable = context.Compiler.Compile(spec.Identifier,
            $$"""
              #include <string>
              #include <iostream>

              {{spec.Source}}

              int main(int argc, char* argv[])
              {
                  return {{spec.Identifier}}::contains({{CodeHelper.ToValueLabel(data[0])}});
              }
              """);

        Assert.Equal(1, RunProcess(executable));
    }

    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public sealed class CPlusPlusContext
    {
        public CPlusPlusCompiler Compiler { get; } = new CPlusPlusCompiler(false);
    }
}