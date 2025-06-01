using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Xunit.Abstractions;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestVector<T>(Type type, T[] values, IStringHash? stringHash = null) : ITestVector, IXunitSerializable
{
    private readonly DataType _dataType = Enum.Parse<DataType>(typeof(T).Name);

    public T[] Values { get; private set; } = values;
    public Type Type { get; private set; } = type;
    public IStringHash? StringHash { get; private set; } = stringHash;

    public string Identifier => $"{Type.Name.Replace("`1", "", StringComparison.Ordinal)}_{_dataType}_{Values.Length}";

    public override string ToString() => Identifier;

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Type), Type);
        info.AddValue(nameof(Values), Values);
        info.AddValue(nameof(StringHash), StringHash);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Type = info.GetValue<Type>(nameof(Type));
        Values = info.GetValue<T[]>(nameof(Values));
        StringHash = info.GetValue<IStringHash>(nameof(StringHash));
    }
}