using Genbox.FastData.Config;
using Genbox.FastData.Generator;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Abstracts;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestData<TKey>(Type structureType, TKey[] keys) : ITestData, IXunitSerializable
{
    private readonly TypeCode _keyType = Type.GetTypeCode(typeof(TKey));
    private readonly Random _rng = new Random(42);

    public TKey[] Keys { get; private set; } = keys;
    public Type StructureType { get; private set; } = structureType;
    public int WarmupIterations { get; } = 1_000_000;
    public int WorkIterations { get; } = 1_000_000;
    public int QueryCount { get; } = 25;

    public string Identifier => $"{StructureType}_{_keyType}_{Keys.Length}";

    public string Generate(ICodeGenerator generator)
    {
        if (Keys is string[] strArr)
            return FastDataGenerator.Generate(strArr, new StringDataConfig { StructureTypeOverride = StructureType }, generator);

        return FastDataGenerator.Generate(Keys, new NumericDataConfig { StructureTypeOverride = StructureType }, generator);
    }

    public string GetRandomKey(TypeMap map) => map.ToValueLabel(Keys[_rng.Next(0, Keys.Length)]);

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(StructureType), StructureType);
        info.AddValue(nameof(Keys), Keys);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        StructureType = info.GetValue<Type>(nameof(StructureType));
        Keys = info.GetValue<TKey[]>(nameof(Keys));
    }

    public override string ToString() => Identifier;
}