using System.Text.Json;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Newtonsoft.Json;

namespace Genbox.FastData.Tests;

public class FastDataGeneratorTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public async Task TryGenerateTest(ITestData testData)
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

    private sealed class DummyGenerator : ICodeGenerator
    {
        public bool UseUTF16Encoding => true;

        public bool TryGenerate<T>(GeneratorConfig<T> genCfg, IContext context, out string? source)
        {
            source = JsonConvert.SerializeObject(context, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            });
            return true;
        }
    }
}