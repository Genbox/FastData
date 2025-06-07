using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.Tests;

public class GeneratorTests
{
    private readonly RustCodeGenerator _generator = RustCodeGenerator.Create(new RustCodeGeneratorConfig("MyData"));

    [Theory]
    [ClassData(typeof(TestTheoryData))]
    internal async Task GenerateStructureType<T>(TestData<T> data)
    {
        Assert.True(TryGenerate(_ => _generator, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("DataStructures")
              .DisableDiff();
    }
}