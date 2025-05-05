using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;

namespace Genbox.FastData.Generator.Tests;

public class GenericTests
{
    [Fact]
    public void OutputIsUniq()
    {
        FastDataConfig config = new FastDataConfig();
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.TryGenerate(["item", "item"], config, new DummyGenerator(), out _));
    }

    [Fact]
    public Task VerifyChecksTest() => VerifyChecks.Run();

    private class DummyGenerator : ICodeGenerator
    {
        public bool UseUTF16Encoding => false;
        public bool TryGenerate<T>(GeneratorConfig genCfg, IContext context, out string? source) => throw new NotSupportedException();
    }
}