using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.Rust.Tests;

public class GeneratorTests
{
    private readonly RustCodeGenerator _generator = new RustCodeGenerator(new RustCodeGeneratorConfig("MyData"));

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