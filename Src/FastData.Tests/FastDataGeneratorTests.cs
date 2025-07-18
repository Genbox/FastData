using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Tests;

public class FastDataGeneratorTests
{
    [Fact]
    public void Generate_ThrowOnDuplicate()
    {
        FastDataConfig config = new FastDataConfig();
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate(["item", "item"], config, new DummyGenerator()));
    }

    [Fact]
    public void Generate_ThrowOnInvalidType()
    {
        Assert.Throws<InvalidOperationException>(() => FastDataGenerator.Generate([DateTime.Now, DateTime.UtcNow], new FastDataConfig(StructureType.Array), new DummyGenerator()));
    }

    // [Fact]
    // public async Task Generate_SpanSupport()
    // {
    //     ReadOnlySpan<int> span = [1, 2, 3, 4, 5];
    //     string source = FastDataGenerator.Generate(span, new FastDataConfig(StructureType.Array), new DummyGenerator());
    //
    //     await Verify(source)
    //           .UseFileName("SupportSpan")
    //           .UseDirectory("Features")
    //           .DisableDiff();
    // }

    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task Generate_CommonInputs(ITestData testData)
    {
        testData.Generate(_ => new DummyGenerator(), out GeneratorSpec spec);
        Assert.NotNull(spec.Source);

        await Verify(spec.Source)
              .UseFileName(spec.Identifier)
              .UseDirectory("Verify")
              .DisableDiff();
    }

    public static TheoryData<ITestData> GetTestData()
    {
        TheoryData<ITestData> data = new TheoryData<ITestData>();
        foreach (StructureType type in Enum.GetValues<StructureType>())
        {
            data.Add(new TestData<string>(type, ["item1", "item2", "item3"]));
            data.Add(new TestData<int>(type, [int.MinValue, 0, int.MaxValue]));
            data.Add(new TestData<long>(type, [long.MinValue, 0, long.MaxValue]));
            data.Add(new TestData<double>(type, [double.MinValue, 0, double.MaxValue]));
        }
        return data;
    }
}