using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.Tests;

public class GenericTests
{
    [Fact]
    public Task VerifyChecksTest() => VerifyChecks.Run();

    [Fact]
    public void KeyedThrowsOnValueCountMismatch()
    {
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.GenerateKeyed([1, 2, 3], [1, 2], new FastDataConfig(), new DummyGenerator()));
    }
}