using Genbox.FastData.Enums;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestVector<TKey, TValue>(Type type, TKey[] keys, TKey[] notPresent, TValue[] values, string? postfix = null) : TestVector<TKey>(type, keys, notPresent, postfix)
{
    public TValue[] Values { get; } = values;
}

public class TestVector<TKey>(Type type, TKey[] keys, TKey[] notPresent, string? postfix = null) : ITestVector, IXunitSerializable
{
    private readonly KeyType _keyType = Enum.Parse<KeyType>(typeof(TKey).Name);

    public TKey[] Keys { get; private set; } = keys;
    public TKey[] NotPresent { get; } = notPresent;
    public Type Type { get; private set; } = type;

    public string Identifier => $"{Type.Name.Replace("`1", "", StringComparison.Ordinal).Replace("`2", "", StringComparison.Ordinal)}_{_keyType}_{Keys.Length}" + (postfix != null ? $"_{postfix}" : "");

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