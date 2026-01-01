using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BinarySearchStructure<TKey, TValue>(KeyType keyType, bool ignoreCase, NumericKeyProperties<TKey>? props) : IStructure<TKey, TValue, BinarySearchContext<TKey, TValue>>
{
    public BinarySearchContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        TKey[] keysCopy = new TKey[keys.Length];
        keys.Span.CopyTo(keysCopy);

        TValue[] valuesCopy = [];

        if (!values.IsEmpty)
        {
            valuesCopy = new TValue[values.Length];
            values.Span.CopyTo(valuesCopy);
        }

        if (keyType == KeyType.String)
        {
            if (!values.IsEmpty)
                Array.Sort(keysCopy, valuesCopy, StringHelper.GetStringComparer(ignoreCase));
            else
                Array.Sort(keysCopy, StringHelper.GetStringComparer(ignoreCase));
        }
        else
        {
            if (!values.IsEmpty)
                Array.Sort(keysCopy, valuesCopy);
            else
                Array.Sort(keysCopy);
        }

        // At this point, the data is sorted, and we can measure equidistance
        bool useInterpolation = false;

        if (keyType != KeyType.String && props != null)
            useInterpolation = IsWellDistributed(keysCopy.AsSpan(), props);

        return new BinarySearchContext<TKey, TValue>(keysCopy, valuesCopy, useInterpolation);
    }

    private static bool IsWellDistributed(ReadOnlySpan<TKey> keys, NumericKeyProperties<TKey> props)
    {
        const int bucketLimit = 20;

        if (keys.Length < 16)
            return false;

        int buckets = Math.Min(bucketLimit, keys.Length);
        if (buckets <= 1)
            return false;

        ulong range = props.Range;
        if (range == 0)
            return false;

        Span<int> hist = stackalloc int[buckets];
        Func<TKey, long> converter = props.ValueConverter;
        ulong min = (ulong)converter(props.MinKeyValue);

        foreach (TKey key in keys)
        {
            ulong value = (ulong)converter(key);
            ulong diff = value - min;
            int i = (int)((diff * (ulong)buckets) / range);

            if ((uint)i >= (uint)buckets)
                i = buckets - 1;

            hist[i]++;
        }

        int minCount = int.MaxValue;
        int maxCount = 0;
        int sum = 0;

        for (int i = 0; i < buckets; i++)
        {
            int count = hist[i];
            sum += count;
            if (count < minCount)
                minCount = count;
            if (count > maxCount)
                maxCount = count;
        }

        if (minCount == 0)
            return false;

        int avg = sum / buckets;
        return maxCount - minCount <= avg;
    }
}