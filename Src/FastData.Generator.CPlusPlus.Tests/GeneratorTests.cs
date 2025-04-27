using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class GeneratorTests
{
    private readonly CPlusPlusCodeGenerator _generator = new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig("my_data"));

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