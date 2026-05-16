namespace Genbox.FastData.Internal.Pgm;

/// <summary>Builds a PGM index over sorted data. Assumes the input has at least two sorted keys, does not contain the sentinel key, and uses a positive epsilon.</summary>
/// <remarks>
/// Based on the reference PGM-index implementation (pgm_index.hpp, piecewise_linear_model.hpp).
/// Uses C# Parallel.For instead of OpenMP for parallel segmentation.
/// Negative intercepts are clamped to 0 rather than throwing (reference throws).
/// sentinelMinusOne for float types uses BitDecrement instead of sentinel-1 (which in the reference evaluates to infinity).
/// </remarks>
internal static class PgmBuilder<T> where T : notnull
{
    private static readonly T _sentinel = PgmTypeTraits<T>.MaxValue;

    /// <summary>Builds a single-level PGM index over the given sorted keys.</summary>
    /// <param name="data">The sorted input keys (as a memory region to avoid ref-struct capture issues).</param>
    /// <param name="epsilon">The maximum approximation error. A smaller value produces more segments but tighter search bounds.</param>
    /// <returns>The list of segments for the PGM index.</returns>
    public static PgmSegment<T>[] Build(ReadOnlyMemory<T> data, int epsilon)
    {
        Result result = BuildIndex(data, epsilon, 0);
        PgmSegment<T>[] segments = new PgmSegment<T>[result.SegmentCount];
        Array.Copy(result.Segments, segments, result.SegmentCount);
        return segments;
    }

    /// <summary>Builds a PGM index over the given sorted keys.</summary>
    /// <param name="data">The sorted input keys (as a memory region to avoid ref-struct capture issues).</param>
    /// <param name="epsilon">The maximum approximation error. A smaller value produces more segments but tighter search bounds.</param>
    /// <param name="epsilonRecursive">The maximum approximation error for recursive index levels.</param>
    /// <returns>The PGM index segments and level metadata.</returns>
    public static Result BuildIndex(ReadOnlyMemory<T> data, int epsilon, int epsilonRecursive)
    {
        List<PgmSegment<T>> segments = new List<PgmSegment<T>>();
        List<int> levelsOffsets = new List<int>();
        BuildSegments(data, epsilon, epsilonRecursive, segments, levelsOffsets);
        return CreateResult(segments, levelsOffsets);
    }

    private static Result CreateResult(List<PgmSegment<T>> segments, List<int> levelsOffsets)
    {
        int segmentCount = levelsOffsets[1] - 1;
        return new Result(segments.ToArray(), levelsOffsets.ToArray(), segmentCount);
    }

    private static void BuildSegments(ReadOnlyMemory<T> data, int epsilon, int epsilonRecursive, List<PgmSegment<T>> segments, List<int> levelsOffsets)
    {
        int n = data.Length;
        levelsOffsets.Add(0);

        int BuildLevel(int eps, Func<int, T> input, Action<OptimalPiecewiseLinearModel<T>.CanonicalSegment> output, int lastN)
        {
            int nSegments = MakeSegmentationPar(lastN, eps, input, output);
            if (segments.Count > 0 && Comparer<T>.Default.Compare(segments[segments.Count - 1].Key, _sentinel) == 0)
                nSegments--;
            else
            {
                T lastKey = data.Span[n - 1];

                // The reference uses `sentinel - 1` for the gap check, which for floating-point
                // sentinel (infinity) is still infinity. We use the previous representable value instead.
                T sentinelMinusOne = PgmTypeTraits<T>.IsFloatingPoint
                    ? PgmTypeTraits<T>.PreviousValue(_sentinel)
                    : PgmTypeTraits<T>.FromInt128(PgmTypeTraits<T>.ToInt128(_sentinel) - 1);
                if (segments[segments.Count - 1].Evaluate(sentinelMinusOne) < lastN)
                    segments.Add(new PgmSegment<T>(PgmTypeTraits<T>.AddOne(lastKey), 0, lastN));
                segments.Add(new PgmSegment<T>(_sentinel, 0, lastN));
            }

            return nSegments;
        }

        Action<OptimalPiecewiseLinearModel<T>.CanonicalSegment> outputFirst = cs => segments.Add(new PgmSegment<T>(cs));
        int last = BuildLevel(epsilon, i => data.Span[i], outputFirst, n);
        levelsOffsets.Add(segments.Count);

        while (epsilonRecursive > 0 && last > 1)
        {
            int offset = levelsOffsets[levelsOffsets.Count - 2];
            last = BuildLevel(epsilonRecursive, i => segments[offset + i].Key, outputFirst, last);
            levelsOffsets.Add(segments.Count);
        }
    }

    private static int MakeSegmentation(int n, int start, int end, int epsilon, Func<int, T> input, Action<OptimalPiecewiseLinearModel<T>.CanonicalSegment> output)
    {
        int c = 0;
        OptimalPiecewiseLinearModel<T> opt = new OptimalPiecewiseLinearModel<T>(epsilon);

        void AddPoint(T x, int y)
        {
            if (!opt.AddPoint(x, y))
            {
                output(opt.GetSegment());
                opt.AddPoint(x, y);
                c++;
            }
        }

        AddPoint(input(start), start);
        for (int i = start + 1; i < end - 1; i++)
        {
            if (Comparer<T>.Default.Compare(input(i), input(i - 1)) == 0)
            {
                if (PgmTypeTraits<T>.IsFloatingPoint)
                {
                    T next = PgmTypeTraits<T>.NextAfter(input(i));
                    if (Comparer<T>.Default.Compare(next, input(i + 1)) < 0)
                        AddPoint(next, i);
                }
                else
                {
                    T next = PgmTypeTraits<T>.AddOne(input(i));
                    if (Comparer<T>.Default.Compare(next, input(i + 1)) < 0)
                        AddPoint(next, i);
                }
            }
            else
                AddPoint(input(i), i);
        }

        if (end >= start + 2 && Comparer<T>.Default.Compare(input(end - 1), input(end - 2)) != 0)
            AddPoint(input(end - 1), end - 1);

        if (end == n)
        {
            T tail = PgmTypeTraits<T>.IsFloatingPoint
                ? PgmTypeTraits<T>.NextAfter(input(n - 1))
                : PgmTypeTraits<T>.AddOne(input(n - 1));
            AddPoint(tail, n);
        }

        output(opt.GetSegment());
        return ++c;
    }

    private static int MakeSegmentationPar(int n, int epsilon, Func<int, T> input, Action<OptimalPiecewiseLinearModel<T>.CanonicalSegment> output)
    {
        ThreadPool.GetMaxThreads(out int maxThreads, out _);
        int parallelism = Math.Min(Math.Min(Environment.ProcessorCount, maxThreads), 20);
        int chunkSize = n / parallelism;

        if (parallelism <= 1 || n < 1 << 15)
            return MakeSegmentation(n, 0, n, epsilon, input, output);

        List<OptimalPiecewiseLinearModel<T>.CanonicalSegment>?[] results = new List<OptimalPiecewiseLinearModel<T>.CanonicalSegment>?[parallelism];
        int[] counts = new int[parallelism];

        Parallel.For(0, parallelism, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, i =>
        {
            int first = i * chunkSize;
            int last = i == parallelism - 1 ? n : first + chunkSize;
            if (first > 0)
            {
                for (; first < last; ++first)
                {
                    if (Comparer<T>.Default.Compare(input(first), input(first - 1)) != 0)
                        break;
                }

                if (first == last)
                    return;
            }

            List<OptimalPiecewiseLinearModel<T>.CanonicalSegment> local = new List<OptimalPiecewiseLinearModel<T>.CanonicalSegment>(chunkSize / (epsilon * epsilon));
            results[i] = local;
            counts[i] = MakeSegmentation(n, first, last, epsilon, input, cs => local.Add(cs));
        });

        int total = 0;
        for (int i = 0; i < parallelism; i++)
        {
            total += counts[i];
            List<OptimalPiecewiseLinearModel<T>.CanonicalSegment>? local = results[i];
            if (local is null)
                continue;
            foreach (OptimalPiecewiseLinearModel<T>.CanonicalSegment cs in local)
                output(cs);
        }

        return total;
    }

    internal readonly struct Result(PgmSegment<T>[] segments, int[] levelsOffsets, int segmentCount)
    {
        public PgmSegment<T>[] Segments { get; } = segments;
        public int[] LevelsOffsets { get; } = levelsOffsets;
        public int SegmentCount { get; } = segmentCount;
    }
}