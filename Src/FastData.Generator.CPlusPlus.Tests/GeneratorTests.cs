using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class GeneratorTests
{
    private readonly CPlusPlusCodeGenerator _generator = CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig("my_data"));

    [Theory]
    [ClassData(typeof(TestTheoryData))]
    internal async Task GenerateStructureType<T>(TestData<T> data)
    {
        Assert.True(TestHelper.TryGenerate(_ => _generator, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("DataStructures")
              .DisableDiff();
    }
}