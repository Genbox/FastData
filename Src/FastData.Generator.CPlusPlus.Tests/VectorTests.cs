using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.Shared;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class VectorTests(VectorTests.CPlusPlusContext context) : IClassFixture<VectorTests.CPlusPlusContext>
{
    [Theory]
    [ClassData(typeof(TestVectorTheoryData))]
    public void Test<T>(TestVector<T> data)
    {
        Assert.True(TestVectorHelper.TryGenerate(id => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id)), data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeHelper helper = new TypeHelper(new TypeMap(langDef.TypeDefinitions));

        string source = $$"""
                          #include <string>
                          #include <iostream>

                          {{spec.Source}}

                          int main(int argc, char* argv[])
                          {
                          {{FormatList(data.Values, x => $"""
                                                          if (!{spec.Identifier}::contains({helper.ToValueLabel(x)}))
                                                              return false;
                                                          """, "\n")}}

                              return 1;
                          }
                          """;

        string executable = context.Compiler.Compile(spec.Identifier, source);

        Assert.Equal(1, RunProcess(executable));
    }

    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    [SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class CPlusPlusContext
    {
        public CPlusPlusContext()
        {
            string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");
            Directory.CreateDirectory(rootDir);
            Compiler = new CPlusPlusCompiler(false, rootDir);
        }

        public CPlusPlusCompiler Compiler { get; }
    }
}