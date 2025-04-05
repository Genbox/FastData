using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;

namespace Genbox.FastData.Generator.Tests;

public class GenericTests
{
    [Fact]
    public void OutputIsUniq()
    {
        FastDataConfig config = new FastDataConfig("MyData", ["item", "item"]);
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.TryGenerate(config, new DummyGenerator(), out _));
    }

    private class DummyGenerator : IGenerator
    {
        public string Generate(GeneratorConfig genCfg, FastDataConfig fastCfg, IContext context) => throw new NotSupportedException();
    }
}