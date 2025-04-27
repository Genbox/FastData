using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.Rust.Tests;

public class GeneratorTests
{
    private readonly RustCodeGenerator _generator = new RustCodeGenerator(new RustGeneratorConfig("MyData"));

    [Theory]
    [ClassData(typeof(TestDataClass))]
    internal async Task GenerateStructureType(StructureType structureType, object[] data)
    {
        if (!TestVectorHelper.TryGenerate(_ => _generator, structureType, data, out GeneratorSpec spec))
            return;

        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Verify")
              .DisableDiff();
    }
}