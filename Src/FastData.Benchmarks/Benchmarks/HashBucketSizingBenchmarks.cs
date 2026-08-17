using BenchmarkDotNet.Order;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class HashBucketSizingBenchmarks
{
    private ulong[] _hashCodes = null!;

    [Params(32, 256, 1_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _hashCodes = new ulong[Count];
        ulong state = 0x9e3779b97f4a7c15UL;

        for (int i = 0; i < _hashCodes.Length; i++)
        {
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            unchecked { _hashCodes[i] = state * 0x2545f4914f6cdd1dUL; }
        }

        (int LegacyLength, int LegacyCollisions) legacy = CurrentSwitchingBitSet();
        (int FastDataLength, int FastDataCollisions) fastData = FastDataWordBitSet();
        if (legacy != fastData)
            throw new InvalidOperationException($"Sizing implementations disagree: {legacy} != {fastData}.");
    }

    [Benchmark(Baseline = true)]
    public (int Length, int Collisions) CurrentSwitchingBitSet() => FindTableSizeCurrent(_hashCodes);

    [Benchmark]
    public (int Length, int Collisions) FastDataWordBitSet()
    {
        int length = HashData.GetOptimizedBucketTableSize(Count, _hashCodes, out int collisions);
        return (length, collisions);
    }

    private static (int Length, int Collisions) FindTableSizeCurrent(ReadOnlySpan<ulong> hashCodes)
    {
        int collisions = CountBucketCollisionsCurrent(hashCodes, hashCodes.Length);
        if (collisions == 0)
            return (hashCodes.Length, collisions);

        int maxLength = GetMaxLength(hashCodes.Length);
        int bestLength = hashCodes.Length;

        for (int candidate = hashCodes.Length + 1; candidate <= maxLength; candidate++)
        {
            int candidateCollisions = CountBucketCollisionsCurrent(hashCodes, candidate);
            if (candidateCollisions >= collisions)
                continue;

            bestLength = candidate;
            collisions = candidateCollisions;

            if (candidateCollisions / (double)hashCodes.Length <= 0.05)
                break;
        }

        return (bestLength, collisions);
    }

    private static int GetMaxLength(int count)
    {
        int multiplier = count >= 1_000 ? 3 : 16;
        long maxByMultiplier = (long)count * multiplier;
        long maxByCandidates = (long)count + 256;
        return (int)Math.Min(int.MaxValue, Math.Max(count, Math.Min(maxByMultiplier, maxByCandidates)));
    }

    private static int CountBucketCollisionsCurrent(ReadOnlySpan<ulong> hashCodes, int length)
    {
        SwitchingBitSet tracker = new SwitchingBitSet(length, false);
        int collisions = 0;

        for (int i = 0; i < hashCodes.Length; i++)
        {
            if (!tracker.Add((uint)(hashCodes[i] % (uint)length)))
                collisions++;
        }

        return collisions;
    }
}