using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class GeneratorTests
{
    private readonly CPlusPlusCodeGenerator _generator = new CPlusPlusCodeGenerator(new CPlusPlusCodeGeneratorConfig("my_data"));

    [Theory]
    [ClassData(typeof(TestDataClass))]
    internal async Task GenerateStructureType(ITestData data)
    {
        Assert.True(TestVectorHelper.TryGenerate(_ => _generator, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Verify")
              .DisableDiff();
    }
}