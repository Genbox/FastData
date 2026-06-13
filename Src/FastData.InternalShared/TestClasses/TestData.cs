using Genbox.FastData.Config;
using Genbox.FastData.Generator;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Extensions;
using Genbox.FastData.Internal.Structures;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

public class TestData<TKey>(Type structureType, TKey[] keys, BenchmarkWorkload workload, int warmupCount = 5, int minSampleCount = 10, int maxSampleCount = 10, int targetIterationTimeMs = 100, double maxErrorPercent = 2.0d) : ITestData, IXunitSerializable
{
    private readonly TypeCode _keyType = Type.GetTypeCode(typeof(TKey));

    public TKey[] Keys { get; private set; } = keys;
    public BenchmarkWorkload Workload { get; private set; } = workload;
    public Type StructureType { get; private set; } = structureType;
    public double MaxErrorPercent { get; } = maxErrorPercent;
    public int TargetIterationTimeMs { get; } = targetIterationTimeMs;
    public Type KeyType => typeof(TKey);
    public int QueryCount => Workload == BenchmarkWorkload.Mixed ? Math.Max(2, Keys.Length) : Math.Max(1, Keys.Length);
    public int WarmupCount { get; } = warmupCount;
    public int MinSampleCount { get; } = minSampleCount;
    public int MaxSampleCount { get; } = maxSampleCount;

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
        bool[] hitQueries = CreateHitQueries(rng);
        TKey[] hitKeys = Workload == BenchmarkWorkload.Miss ? [] : CreateHitKeys(rng, ExpectedFoundCount);
        TKey[] missKeys = Workload == BenchmarkWorkload.Hit ? [] : CreateMissKeys();
        int hitIndex = 0;
        int missIndex = 0;

        for (int i = 0; i < queryKeys.Length; i++)
        {
            TKey key = hitQueries[i] ? hitKeys[hitIndex++ % hitKeys.Length] : missKeys[missIndex++ % missKeys.Length];
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

    private TKey[] CreateMissKeys()
    {
        HashSet<TKey> keySet = new HashSet<TKey>(Keys);
        TKey[] misses = new TKey[Math.Min(QueryCount, Math.Max(1, Keys.Length))];

        for (int i = 0; i < misses.Length; i++)
            misses[i] = BenchmarkMissKeyFactory<TKey>.Create(Keys[i % Keys.Length], keySet, i);

        return misses;
    }

    private TKey[] CreateHitKeys(Random rng, int hitCount)
    {
        TKey[] shuffledKeys = (TKey[])Keys.Clone();
        Shuffle(shuffledKeys, rng);

        if (hitCount <= shuffledKeys.Length)
            return shuffledKeys[..hitCount];

        TKey[] hitKeys = new TKey[hitCount];
        for (int i = 0; i < hitKeys.Length; i++)
            hitKeys[i] = shuffledKeys[i % shuffledKeys.Length];

        return hitKeys;
    }

    private bool[] CreateHitQueries(Random rng)
    {
        bool[] hitQueries = new bool[QueryCount];

        if (Workload == BenchmarkWorkload.Miss)
            return hitQueries;

        int hitCount = Workload == BenchmarkWorkload.Hit ? QueryCount : ExpectedFoundCount;

        for (int i = 0; i < hitCount; i++)
            hitQueries[i] = true;

        Shuffle(hitQueries, rng);
        return hitQueries;
    }

    private static void Shuffle<TValue>(TValue[] values, Random rng)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}