using Genbox.FastData.Enums;
using Xunit.Abstractions;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestVector<TKey>(Type type, TKey[] keys, TKey[] notPresent, string? postfix = null) : ITestVector, IXunitSerializable
{
    private readonly DataType _dataType = Enum.Parse<DataType>(typeof(TKey).Name);

    public TKey[] Keys { get; private set; } = keys;
    public TKey[] NotPresent { get; } = notPresent;
    public Type Type { get; private set; } = type;

    public string Identifier => $"{Type.Name.Replace("`1", "", StringComparison.Ordinal).Replace("`2", "", StringComparison.Ordinal)}_{_dataType}_{Keys.Length}" + (postfix != null ? $"_{postfix}" : "");

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Type), Type);
        info.AddValue(nameof(Keys), Keys);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Type = info.GetValue<Type>(nameof(Type));
        Keys = info.GetValue<TKey[]>(nameof(Keys));
    }

    public override string ToString() => Identifier;
}