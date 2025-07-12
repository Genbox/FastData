using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.Tests;

public class GeneratorTests
{
    [Fact]
    public void KeyedThrowsOnValueCountMismatch()
    {
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.GenerateKeyed([1, 2, 3], [1, 2], new FastDataConfig(), new DummyGenerator()));
    }

    [Fact]
    public async Task StructSupportTest()
    {
        string source = FastDataGenerator.GenerateKeyed([1, 2, 3], [
            new X { Age = 1, Name = "Bob" },
            new X { Age = 2, Name = "Billy" },
            new X { Age = 3, Name = "Bibi" },
        ], new FastDataConfig(), new DummyGenerator());

        await Verify(source)
              .UseFileName(nameof(StructSupportTest))
              .UseDirectory("Verify")
              .DisableDiff();
    }

    internal struct X
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}