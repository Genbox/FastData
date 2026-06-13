namespace Genbox.FastData.InternalShared.TestClasses;

internal static class BenchmarkQueryHelper
{
    public static int GetExpectedFoundCount(BenchmarkWorkload workload, int queryCount) => workload switch
    {
        BenchmarkWorkload.Hit => queryCount,
        BenchmarkWorkload.Miss => 0,
        BenchmarkWorkload.Mixed => (queryCount + 1) / 2,
        _ => throw new InvalidOperationException($"Unsupported benchmark workload '{workload}'.")
    };

    public static bool[] CreateHitQueries(BenchmarkWorkload workload, int queryCount, int expectedFoundCount, Random rng)
    {
        bool[] hitQueries = new bool[queryCount];

        if (workload == BenchmarkWorkload.Miss)
            return hitQueries;

        int hitCount = workload == BenchmarkWorkload.Hit ? queryCount : expectedFoundCount;

        for (int i = 0; i < hitCount; i++)
            hitQueries[i] = true;

        Shuffle(hitQueries, rng);
        return hitQueries;
    }

    public static void Shuffle<T>(T[] values, Random rng)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}