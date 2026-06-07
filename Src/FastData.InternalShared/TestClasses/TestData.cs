using Genbox.FastData.Config;
using Genbox.FastData.Generator;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Extensions;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestData<TKey>(Type structureType, TKey[] keys) : ITestData, IXunitSerializable
{
    private readonly TypeCode _keyType = Type.GetTypeCode(typeof(TKey));

    public TKey[] Keys { get; private set; } = keys;
    public Type StructureType { get; private set; } = structureType;
    public int WorkIterations { get; } = 1_000_000;
    public int QueryCount { get; } = 25;
    public int WarmupSampleCount { get; } = 2;
    public int MeasurementSampleCount { get; } = 5;

    public string Identifier => $"{StructureType.GetFriendlyName()}_{_keyType}_{Keys.Length}";

    public string Generate(ICodeGenerator generator)
    {
        if (Keys is string[] strArr)
            return FastDataGenerator.Generate(strArr, new StringDataConfig { StructureTypeOverride = StructureType }, generator);

        return FastDataGenerator.Generate(Keys, new NumericDataConfig { StructureTypeOverride = StructureType }, generator);
    }

    public string[] GetQuerySet(TypeMap map)
    {
        Random rng = new Random(42);
        string[] queryKeys = new string[QueryCount];

        for (int i = 0; i < queryKeys.Length; i++)
            queryKeys[i] = map.ToValueLabel(Keys[rng.Next(0, Keys.Length)]);

        return queryKeys;
    }

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