using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class GeneratorTests
{
    private readonly CSharpCodeGenerator _generator = CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig("MyData"));

    [Theory]
    [ClassData(typeof(TestDataClass))]
    internal async Task GenerateStructureType<T>(TestData<T> data)
    {
        Assert.True(TestVectorHelper.TryGenerate(_ => _generator, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("DataStructures")
              .DisableDiff();
    }
}