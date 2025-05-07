using Genbox.FastData.Enums;
using Xunit.Abstractions;

namespace Genbox.FastData.InternalShared;

public class TestData<T> : ITestData, IXunitSerializable
{
    public TestData(StructureType structureType, T[] values)
    {
        StructureType = structureType;
        Values = values;
    }

    public Type Type => typeof(T);
    public int Length => Values.Length;
    public object[] Items => Values.Cast<object>().ToArray();

    public string Identifier
    {
        get
        {
            DataType dataType = Enum.Parse<DataType>(Type.Name);
            return $"{StructureType}_{dataType}_{Length}";
        }
    }

    public StructureType StructureType { get; set; }
    public T[] Values { get; set; }

    public override string ToString() => Identifier;

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