using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Xunit.Abstractions;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestData<T>(StructureType structureType, T[] values) : ITestData, IXunitSerializable
{
    private readonly DataType _dataType = Enum.Parse<DataType>(typeof(T).Name);
    private readonly Random _rng = new Random(42);

    public T[] Values { get; private set; } = values;
    public StructureType StructureType { get; private set; } = structureType;

    public string Identifier => $"{StructureType}_{_dataType}_{Values.Length}";

    public void Generate(Func<string, ICodeGenerator> factory, out GeneratorSpec spec)
    {
        string source = FastDataGenerator.Generate(Values, new FastDataConfig(StructureType) { StringAnalyzerConfig = null }, factory(Identifier));
        spec = new GeneratorSpec(Identifier, source, GeneratorFlags.None);
    }

    public string GetValueLabel(TypeMap map) => map.ToValueLabel(Values[_rng.Next(0, Values.Length)]);

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(StructureType), StructureType);
        info.AddValue(nameof(Values), Values);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        StructureType = info.GetValue<StructureType>(nameof(StructureType));
        Values = info.GetValue<T[]>(nameof(Values));
    }

    public override string ToString() => Identifier;
}