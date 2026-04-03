using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal;

internal static class NumericAnalyzer
{
    internal static bool IsWellDistributed<TKey>(ReadOnlySpan<TKey> keys, NumericKeyProperties<TKey> props, int maxHistogramBuckets)
    {
        if (keys.Length < 16)
            return false;

        int buckets = Math.Min(maxHistogramBuckets, keys.Length);
        if (buckets <= 1)
            return false;

        if (props.Range == 0)
            return false;

        Span<int> hist = stackalloc int[buckets];
        Func<TKey, long> conv = Type.GetTypeCode(typeof(TKey)).GetSignedValueConverter<TKey>();
        ulong min = (ulong)conv(props.DataRanges.Min);

        if (typeof(TKey) == typeof(float))
        {
            for (int i = 0; i < keys.Length; i++)
            {
                float key = (float)(object)keys[i]!;
                if (float.IsNaN(key) || float.IsInfinity(key))
                    return false;

                ulong value = (ulong)(long)key;
                ulong diff = value - min;
                int bucketIndex = (int)((diff * (ulong)buckets) / props.Range);

                if ((uint)bucketIndex >= (uint)buckets)
                    bucketIndex = buckets - 1;

                hist[bucketIndex]++;
            }
        }
        else if (typeof(TKey) == typeof(double))
        {
            for (int i = 0; i < keys.Length; i++)
            {
                double key = (double)(object)keys[i]!;
                if (double.IsNaN(key) || double.IsInfinity(key))
                    return false;

                ulong value = (ulong)(long)key;
                ulong diff = value - min;
                int bucketIndex = (int)((diff * (ulong)buckets) / props.Range);

                if ((uint)bucketIndex >= (uint)buckets)
                    bucketIndex = buckets - 1;

                hist[bucketIndex]++;
            }
        }
        else
        {
            for (int i = 0; i < keys.Length; i++)
            {
                ulong value = (ulong)conv(keys[i]);
                ulong diff = value - min;
                int bucketIndex = (int)((diff * (ulong)buckets) / props.Range);

                if ((uint)bucketIndex >= (uint)buckets)
                    bucketIndex = buckets - 1;

                hist[bucketIndex]++;
            }
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