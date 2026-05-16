using System.Diagnostics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Extensions;
using Genbox.FastData.Internal.Pgm;

namespace Genbox.FastData.Internal.Structures;

public sealed class PgmStructure<TKey, TValue> : IStructure<TKey, TValue, PgmContext<TKey, TValue>> where TKey : notnull
{
    private static readonly TKey _sentinel = PgmTypeTraits<TKey>.MaxValue;

    private readonly int _epsilon;
    private readonly int _epsilonRecursive;
    private readonly bool _keysAreSorted;

    internal PgmStructure(bool keysAreSorted, int epsilon = 64, int epsilonRecursive = 4)
    {
        Debug.Assert(epsilon > 0, "PgmStructure requires a positive epsilon.");
        Debug.Assert(epsilonRecursive >= 0, "PgmStructure requires a non-negative recursive epsilon.");
        Debug.Assert(typeof(TKey) == typeof(int) || typeof(TKey) == typeof(uint) || typeof(TKey) == typeof(long) || typeof(TKey) == typeof(ulong) || typeof(TKey) == typeof(short) || typeof(TKey) == typeof(ushort) || typeof(TKey) == typeof(byte) || typeof(TKey) == typeof(sbyte) || typeof(TKey) == typeof(char) || typeof(TKey) == typeof(float) || typeof(TKey) == typeof(double), "Unsupported key type");

        _keysAreSorted = keysAreSorted;
        _epsilon = epsilon;
        _epsilonRecursive = epsilonRecursive;
    }

    public PgmContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        Debug.Assert(!keys.IsEmpty, "PgmStructure requires at least one key.");
        Debug.Assert(values.IsEmpty || values.Length == keys.Length, "PgmStructure requires value count to match key count when values are present.");
        Debug.Assert(!_keysAreSorted || keys.IsSorted(), "PgmStructure requires sorted input when keysAreSorted is true.");

        if (!_keysAreSorted)
        {
            TKey[] keysCopy = new TKey[keys.Length];
            keys.CopyTo(keysCopy);

            TValue[] valuesCopy = new TValue[values.Length];
            values.CopyTo(valuesCopy);

            if (values.IsEmpty)
                Array.Sort(keysCopy);
            else
                Array.Sort(keysCopy, valuesCopy);

            keys = keysCopy;
            values = valuesCopy;
        }

        if (Comparer<TKey>.Default.Compare(keys.Span[keys.Length - 1], PgmTypeTraits<TKey>.MaxValue) == 0)
            throw new ArgumentException($"The value {_sentinel} is reserved as a sentinel.", nameof(keys));

        List<PgmSegment<TKey>> segments = new List<PgmSegment<TKey>>();
        List<int> levelsOffsets = new List<int>();
        BuildSegments(keys, _epsilon, _epsilonRecursive, segments, levelsOffsets);

        Debug.Assert(levelsOffsets.Count >= 2, "PgmStructure result creation requires at least one completed index level.");
        Debug.Assert(segments.Count >= levelsOffsets[1], "PgmStructure result creation requires level offsets to fit in the segment list.");
        return new PgmContext<TKey, TValue>(keys, values, segments.ToArray(), levelsOffsets.ToArray(), _epsilon, _epsilonRecursive, levelsOffsets[1] - 1);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];

    private static void BuildSegments(ReadOnlyMemory<TKey> data, int epsilon, int epsilonRecursive, List<PgmSegment<TKey>> segments, List<int> levelsOffsets)
    {
        Debug.Assert(!data.IsEmpty, "PgmStructure segment construction requires at least one key.");
        Debug.Assert(epsilon > 0, "PgmStructure segment construction requires a positive epsilon.");
        Debug.Assert(epsilonRecursive >= 0, "PgmStructure segment construction requires a non-negative recursive epsilon.");

        int n = data.Length;
        levelsOffsets.Add(0);

        int BuildLevel(int eps, Func<int, TKey> input, Action<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment> output, int lastN)
        {
            int nSegments = MakeSegmentationPar(lastN, eps, input, output);
            if (segments.Count > 0 && Comparer<TKey>.Default.Compare(segments[segments.Count - 1].Key, _sentinel) == 0)
                nSegments--;
            else
            {
                TKey lastKey = data.Span[n - 1];

                TKey sentinelMinusOne = PgmTypeTraits<TKey>.IsFloatingPoint
                    ? PgmTypeTraits<TKey>.PreviousValue(_sentinel)
                    : PgmTypeTraits<TKey>.FromInt128(PgmTypeTraits<TKey>.ToInt128(_sentinel) - 1);
                if (segments[segments.Count - 1].Evaluate(sentinelMinusOne) < lastN)
                    segments.Add(new PgmSegment<TKey>(PgmTypeTraits<TKey>.AddOne(lastKey), 0, lastN));
                segments.Add(new PgmSegment<TKey>(_sentinel, 0, lastN));
            }

            return nSegments;
        }

        Action<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment> outputFirst = cs => segments.Add(new PgmSegment<TKey>(cs));
        int last = BuildLevel(epsilon, i => data.Span[i], outputFirst, n);
        levelsOffsets.Add(segments.Count);

        while (epsilonRecursive > 0 && last > 1)
        {
            int offset = levelsOffsets[levelsOffsets.Count - 2];
            last = BuildLevel(epsilonRecursive, i => segments[offset + i].Key, outputFirst, last);
            levelsOffsets.Add(segments.Count);
        }
    }

    private static int MakeSegmentation(int n, int start, int end, int epsilon, Func<int, TKey> input, Action<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment> output)
    {
        Debug.Assert(n > 0, "PGM segmentation requires a positive item count.");
        Debug.Assert(start >= 0 && start < end && end <= n, "PGM segmentation requires a valid input range.");
        Debug.Assert(epsilon > 0, "PGM segmentation requires a positive epsilon.");

        int c = 0;
        OptimalPiecewiseLinearModel<TKey> opt = new OptimalPiecewiseLinearModel<TKey>(epsilon);

        void AddPoint(TKey x, int y)
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
            if (Comparer<TKey>.Default.Compare(input(i), input(i - 1)) == 0)
            {
                if (PgmTypeTraits<TKey>.IsFloatingPoint)
                {
                    TKey next = PgmTypeTraits<TKey>.NextAfter(input(i));
                    if (Comparer<TKey>.Default.Compare(next, input(i + 1)) < 0)
                        AddPoint(next, i);
                }
                else
                {
                    TKey next = PgmTypeTraits<TKey>.AddOne(input(i));
                    if (Comparer<TKey>.Default.Compare(next, input(i + 1)) < 0)
                        AddPoint(next, i);
                }
            }
            else
                AddPoint(input(i), i);
        }

        if (end >= start + 2 && Comparer<TKey>.Default.Compare(input(end - 1), input(end - 2)) != 0)
            AddPoint(input(end - 1), end - 1);

        if (end == n)
        {
            TKey tail = PgmTypeTraits<TKey>.IsFloatingPoint
                ? PgmTypeTraits<TKey>.NextAfter(input(n - 1))
                : PgmTypeTraits<TKey>.AddOne(input(n - 1));
            AddPoint(tail, n);
        }

        output(opt.GetSegment());
        return ++c;
    }

    private static int MakeSegmentationPar(int n, int epsilon, Func<int, TKey> input, Action<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment> output)
    {
        Debug.Assert(n > 0, "Parallel PGM segmentation requires a positive item count.");
        Debug.Assert(epsilon > 0, "Parallel PGM segmentation requires a positive epsilon.");

        ThreadPool.GetMaxThreads(out int maxThreads, out _);
        int parallelism = Math.Min(Math.Min(Environment.ProcessorCount, maxThreads), 20);
        int chunkSize = n / parallelism;

        if (parallelism <= 1 || n < 1 << 15)
            return MakeSegmentation(n, 0, n, epsilon, input, output);

        List<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment>?[] results = new List<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment>?[parallelism];
        int[] counts = new int[parallelism];

        Parallel.For(0, parallelism, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, i =>
        {
            int first = i * chunkSize;
            int last = i == parallelism - 1 ? n : first + chunkSize;
            if (first > 0)
            {
                for (; first < last; ++first)
                {
                    if (Comparer<TKey>.Default.Compare(input(first), input(first - 1)) != 0)
                        break;
                }

                if (first == last)
                    return;
            }

            List<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment> local = new List<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment>(chunkSize / (epsilon * epsilon));
            results[i] = local;
            counts[i] = MakeSegmentation(n, first, last, epsilon, input, cs => local.Add(cs));
        });

        int total = 0;
        for (int i = 0; i < parallelism; i++)
        {
            total += counts[i];
            List<OptimalPiecewiseLinearModel<TKey>.CanonicalSegment>? local = results[i];
            if (local is null)
                continue;
            foreach (OptimalPiecewiseLinearModel<TKey>.CanonicalSegment cs in local)
                output(cs);
        }

        return total;
    }
}