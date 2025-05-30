using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework;
using Xunit.Abstractions;

namespace Genbox.FastData.InternalShared;

public class TestData<T>(StructureType structureType, T[] values) : ITestData, IXunitSerializable
{
    private readonly Random _rng = new Random(42);
    private readonly DataType _dataType = Enum.Parse<DataType>(typeof(T).Name);

    public T[] Values { get; private set; } = values;
    public StructureType StructureType { get; private set; } = structureType;

    public string Identifier => $"{StructureType}_{_dataType}_{Values.Length}";

    public override string ToString() => Identifier;

    public void Generate(Func<string, ICodeGenerator> factory, out GeneratorSpec spec)
    {
        if (!TestVectorHelper.TryGenerate(factory, this, out spec))
            throw new InvalidOperationException("Unable to build " + Identifier);
    }

    public string GetValueLabel(TypeHelper helper) => helper.ToValueLabel(Values[_rng.Next(0, Values.Length)]);

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
}