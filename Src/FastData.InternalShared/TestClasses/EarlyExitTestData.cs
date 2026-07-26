using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generator;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.Helpers;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestClasses;

public sealed class EarlyExitTestData<TKey>(
    IEarlyExit[] exits,
    TKey[] hitKeys,
    TKey[] missKeys,
    GeneratorFunction generatorFunctions,
    string exitName,
    BenchmarkWorkload workload,
    int warmupCount = 5,
    int minSampleCount = 10,
    int maxSampleCount = 10,
    int targetIterationTimeMs = 100,
    double maxErrorPercent = 2.0d) : ITestData, IXunitSerializable
{
    private readonly TypeCode _keyType = Type.GetTypeCode(typeof(TKey));

    public IEarlyExit[] Exits { get; private set; } = exits;
    public TKey[] HitKeys { get; private set; } = hitKeys;
    public TKey[] MissKeys { get; private set; } = missKeys;
    public GeneratorFunction GeneratorFunctions { get; private set; } = generatorFunctions;
    public string ExitName { get; private set; } = exitName;
    public BenchmarkWorkload Workload { get; private set; } = workload;
    public double MaxErrorPercent { get; } = maxErrorPercent;
    public int TargetIterationTimeMs { get; } = targetIterationTimeMs;
    public Type KeyType => typeof(TKey);
    public int QueryCount => Workload == BenchmarkWorkload.Mixed ? Math.Max(2, HitKeys.Length) : Math.Max(1, HitKeys.Length);
    public int WarmupCount { get; } = warmupCount;
    public int MinSampleCount { get; } = minSampleCount;
    public int MaxSampleCount { get; } = maxSampleCount;

    public string Identifier => $"EarlyExit_{ExitName}_{_keyType}_{HitKeys.Length}_{Workload}";

    public string Generate(ICodeGenerator generator)
    {
        // Run exits through the pipeline
        List<IEarlyExit> exitList = EarlyExitPipeline.CombineAndDedup([], Exits);
        EarlyExitPipeline.Optimize<TKey>(exitList);

        ParameterExpression inputKey = Expression.Parameter(typeof(TKey), "key");
        AnnotatedExpr[] annotated = EarlyExitPipeline.Annotate(exitList, inputKey);

        // For string exits, we need to apply the allocation gather and deduplication transforms
        if (typeof(TKey) == typeof(string) && GeneratorFunctions.HasFlag(GeneratorFunction.Length))
        {
            MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(Generators.GeneratorFunctions.Length), [typeof(string)])!;
            ParameterExpression length = Expression.Variable(typeof(int), "length");
            AnnotatedExpr lengthAlloc = AnnotatedExpr.Allocation(Expression.Assign(length, Expression.Call(methodInfo, inputKey)));

            AnnotatedExpr[] combined = [lengthAlloc, ..annotated];
            annotated = ExpressionHelper.Transform(combined,
            [
                new AllocationGatherTransform(),
                new DeduplicateAllocationTransform()
            ]).ToArray();
        }

        EarlyExitOnlyGeneratorConfig config = new EarlyExitOnlyGeneratorConfig(annotated, GeneratorFunctions);
        EarlyExitOnlyContext context = new EarlyExitOnlyContext();

        return generator.Generate<TKey, byte>(config, context);
    }

    public BenchmarkQuerySet GetQuerySet(TypeMap map)
    {
        int expectedFoundCount = BenchmarkQueryHelper.GetExpectedFoundCount(Workload, QueryCount);

        Random rng = new Random(42);
        string[] queryKeys = new string[QueryCount];
        bool[] hitQueries = BenchmarkQueryHelper.CreateHitQueries(Workload, QueryCount, expectedFoundCount, rng);
        int hitIndex = 0;
        int missIndex = 0;

        for (int i = 0; i < queryKeys.Length; i++)
        {
            TKey key = hitQueries[i] ? HitKeys[hitIndex++ % HitKeys.Length] : MissKeys[missIndex++ % MissKeys.Length];
            queryKeys[i] = map.GetValueLiteral(key);
        }

        return new BenchmarkQuerySet(queryKeys, expectedFoundCount, true);
    }

    // Serialization is partial: Exits and RequiredFunctions are not serializable and are
    // only available when the instance is constructed directly. This matches the pattern
    // used by TestData<TKey> where serialization supports display names, not full round-trips.

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(ExitName), ExitName);
        info.AddValue(nameof(HitKeys), HitKeys);
        info.AddValue(nameof(MissKeys), MissKeys);
        info.AddValue(nameof(Workload), Workload);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        ExitName = info.GetValue<string>(nameof(ExitName));
        HitKeys = info.GetValue<TKey[]>(nameof(HitKeys));
        MissKeys = info.GetValue<TKey[]>(nameof(MissKeys));
        Workload = info.GetValue<BenchmarkWorkload>(nameof(Workload));
        Exits = [];
        GeneratorFunctions = GeneratorFunction.None;
    }

    public override string ToString() => Identifier;
}