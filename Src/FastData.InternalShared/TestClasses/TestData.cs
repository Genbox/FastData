using Genbox.FastData.Config;
using Genbox.FastData.Generator;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Extensions;
using Genbox.FastData.Internal.Structures;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestData<TKey>(Type structureType, TKey[] keys, BenchmarkWorkload workload, int warmupCount = 5, int sampleCount = 10, int workIterations = 1_000_000, int queryCount = 25) : ITestData, IXunitSerializable
{
    private readonly TypeCode _keyType = Type.GetTypeCode(typeof(TKey));

    public TKey[] Keys { get; private set; } = keys;
    public BenchmarkWorkload Workload { get; private set; } = workload;
    public Type StructureType { get; private set; } = structureType;
    public int WorkIterations { get; } = workIterations;
    public int QueryCount { get; } = queryCount;
    public int WarmupCount { get; } = warmupCount;
    public int SampleCount { get; } = sampleCount;

    public string Identifier => $"{StructureType.GetFriendlyName()}_{_keyType}_{Keys.Length}_{Workload}";

    public string Generate(ICodeGenerator generator)
    {
        // Benchmark workloads should measure the selected structure, not optional analyzed early exits.
        // Mandatory structure exits still get emitted because they are part of correct operation.
        EarlyExitConfig earlyExitConfig = new EarlyExitConfig { Disabled = true };

        if (Keys is string[] strArr)
            return FastDataGenerator.Generate(strArr, new StringDataConfig { StructureTypeOverride = StructureType, EarlyExitConfig = earlyExitConfig }, generator);

        return FastDataGenerator.Generate(Keys, new NumericDataConfig { StructureTypeOverride = StructureType, EarlyExitConfig = earlyExitConfig }, generator);
    }

    public BenchmarkQuerySet GetQuerySet(TypeMap map)
    {
        Random rng = new Random(42);
        string[] queryKeys = new string[QueryCount];
        TKey[] missKeys = Workload == BenchmarkWorkload.Hit ? [] : CreateMissKeys();

        for (int i = 0; i < queryKeys.Length; i++)
        {
            TKey key = IsHitQuery(i) ? Keys[rng.Next(0, Keys.Length)] : missKeys[rng.Next(0, missKeys.Length)];
            queryKeys[i] = map.ToValueLabel(key);
        }

        return new BenchmarkQuerySet(queryKeys, ExpectedFoundCount, ValidateFoundCount);
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(StructureType), StructureType);
        info.AddValue(nameof(Keys), Keys);
        info.AddValue(nameof(Workload), Workload);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        StructureType = info.GetValue<Type>(nameof(StructureType));
        Keys = info.GetValue<TKey[]>(nameof(Keys));
        Workload = info.GetValue<BenchmarkWorkload>(nameof(Workload));
    }

    public override string ToString() => Identifier;

    private int ExpectedFoundCount => Workload switch
    {
        BenchmarkWorkload.Hit => QueryCount,
        BenchmarkWorkload.Miss => 0,
        BenchmarkWorkload.Mixed => (QueryCount + 1) / 2,
        _ => throw new InvalidOperationException($"Unsupported benchmark workload '{Workload}'.")
    };

    private bool ValidateFoundCount => StructureType != typeof(BloomFilterStructure<,>);

    private bool IsHitQuery(int index) => Workload == BenchmarkWorkload.Hit || (Workload == BenchmarkWorkload.Mixed && index % 2 == 0);

    private TKey[] CreateMissKeys()
    {
        HashSet<TKey> keySet = new HashSet<TKey>(Keys);
        TKey[] misses = new TKey[Math.Min(QueryCount, Math.Max(1, Keys.Length))];

        for (int i = 0; i < misses.Length; i++)
            misses[i] = BenchmarkMissKeyFactory<TKey>.Create(Keys[i % Keys.Length], keySet, i);

        return misses;
    }
}