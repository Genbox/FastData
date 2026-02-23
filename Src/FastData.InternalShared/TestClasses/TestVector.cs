using Genbox.FastData.Extensions;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestVector<TKey, TValue>(Type type, TKey[] keys, TKey[] notPresent, TValue[] values, string? postfix = null) : TestVector<TKey>(type, keys, notPresent, postfix)
{
    public TValue[] Values { get; } = values;
}

public class TestVector<TKey>(Type type, TKey[] keys, TKey[] notPresent, string? postfix = null) : ITestVector
{
    private readonly TypeCode _keyType = Type.GetTypeCode(typeof(TKey));

    public TKey[] Keys { get; } = keys;
    public TKey[] NotPresent { get; } = notPresent;
    public Type Type { get; } = type;

    public string Identifier
    {
        get => field ??= $"{Type.GetCleanName()}_{_keyType}_{Keys.Length}" + (postfix != null ? $"_{postfix}" : "");
        set;
    }

    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Identifier), Identifier);
    public void Deserialize(IXunitSerializationInfo info) => Identifier = info.GetValue<string>(nameof(Identifier));

    public override string ToString() => Identifier;
}