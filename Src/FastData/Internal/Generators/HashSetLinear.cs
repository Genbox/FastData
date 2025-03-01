using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class HashSetLinear(FastDataSpec Spec, GeneratorContext Context) : HashSetCode.IHashSetBase
{
    private Bucket[] _buckets;
    private int[] _hashCodes;
    private object[] _items;

    public void Create(Func<object, uint> hash)
    {
        object[] entries = new object[Spec.Data.Length];

        for (int i = 0; i < Spec.Data.Length; i++)
            entries[i] = Spec.Data[i];

        _items = new object[entries.Length];

        int[] hashCodes = new int[entries.Length];
        for (int i = 0; i < entries.Length; i++)
            hashCodes[i] = (int)hash(entries[i]);

        int numBuckets = CalcNumBuckets(hashCodes, false);
        ulong fastModMultiplier = MathHelper.GetFastModMultiplier((uint)numBuckets);

        int[] arrayPoolBuckets = new int[numBuckets + hashCodes.Length];
        Span<int> bucketStarts = arrayPoolBuckets.AsSpan(0, numBuckets);
        Span<int> nexts = arrayPoolBuckets.AsSpan(numBuckets, hashCodes.Length);
        bucketStarts.Fill(-1);

        for (int index = 0; index < hashCodes.Length; index++)
        {
            int hashCode = hashCodes[index];
            int bucketNum = (int)MathHelper.FastMod((uint)hashCode, (uint)bucketStarts.Length, fastModMultiplier);

            ref int bucketStart = ref bucketStarts[bucketNum];
            nexts[index] = bucketStart;
            bucketStart = index;
        }

        int[] hashtableHashcodes = new int[hashCodes.Length];
        Bucket[] hashtableBuckets = new Bucket[bucketStarts.Length];
        int count = 0;
        for (int bucketNum = 0; bucketNum < hashtableBuckets.Length; bucketNum++)
        {
            int bucketStart = bucketStarts[bucketNum];
            if (bucketStart < 0)
                continue;

            int bucketCount = 0;
            int index = bucketStart;
            bucketStart = count;
            while (index >= 0)
            {
                ref int hashCode = ref hashCodes[index];
                hashtableHashcodes[count] = hashCode;
                hashCode = count;
                count++;
                bucketCount++;

                index = nexts[index];
            }

            hashtableBuckets[bucketNum] = new Bucket(bucketStart, (bucketStart + bucketCount) - 1);
        }

        _hashCodes = hashtableHashcodes;
        _buckets = hashtableBuckets;

        for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
        {
            int destIndex = hashCodes[srcIndex];
            _items[destIndex] = entries[srcIndex];
        }
    }

    public string Generate(IHashSpec? spec) =>
        $$"""
              private{{GetModifier(Spec.ClassType)}} readonly Bucket[] _buckets = {
          {{JoinValues(_buckets, RenderBucket, ",\n")}}
              };

              private{{GetModifier(Spec.ClassType)}} readonly {{Spec.DataTypeName}}[] _items = {
          {{JoinValues(_items, RenderItem, ",\n")}}
              };

              private{{GetModifier(Spec.ClassType)}} readonly int[] _hashCodes = { {{JoinValues(_hashCodes, RenderHashCode)}} };

              {{GetMethodAttributes()}}
              public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
              {
          {{GetEarlyExits("value", Context.GetEarlyExits())}}

                  uint hashCode = {{(spec != null ? "Hash(value)" : GetHashFunction32(Spec.KnownDataType, "value"))}};
                  ref Bucket b = ref _buckets[{{GetModFunction("hashCode", (uint)_buckets.Length)}}];

                  int index = b.StartIndex;
                  int endIndex = b.EndIndex;

                  while (index <= endIndex)
                  {
                      if (hashCode == _hashCodes[index] && {{GetEqualFunction(Spec.KnownDataType, "value", "_items[index]")}})
                          return true;

                      index++;
                  }

                  return false;
              }

          {{(spec != null ? spec.GetSource() : "")}}

              [StructLayout(LayoutKind.Auto)]
              private struct Bucket
              {
                  internal Bucket(int startIndex, int endIndex)
                  {
                      StartIndex = startIndex;
                      EndIndex = endIndex;
                  }

                  internal int StartIndex;
                  internal int EndIndex;
              }
          """;

    private static int CalcNumBuckets(ReadOnlySpan<int> hashCodes, bool hashCodesAreUnique)
    {
        Debug.Assert(hashCodes.Length != 0);
        Debug.Assert(!hashCodesAreUnique || new HashSet<int>(hashCodes.ToArray()).Count == hashCodes.Length);

        const double AcceptableCollisionRate = 0.05;
        const int LargeInputSizeThreshold = 1000;
        const int MaxSmallBucketTableMultiplier = 16;
        const int MaxLargeBucketTableMultiplier = 3;

        HashSet<int>? codes = null;
        int uniqueCodesCount = hashCodes.Length;
        if (!hashCodesAreUnique)
        {
            codes = new HashSet<int>();

            foreach (int hashCode in hashCodes)
                codes.Add(hashCode);
            uniqueCodesCount = codes.Count;
        }
        Debug.Assert(uniqueCodesCount != 0);

        int minNumBuckets = uniqueCodesCount * 2;

        ReadOnlySpan<int> primes = MathHelper.Primes;
        int minPrimeIndexInclusive = 0;
        while ((uint)minPrimeIndexInclusive < (uint)primes.Length && minNumBuckets > primes[minPrimeIndexInclusive])
            minPrimeIndexInclusive++;

        if (minPrimeIndexInclusive >= primes.Length)
            return MathHelper.GetPrime(uniqueCodesCount);

        int maxNumBuckets = uniqueCodesCount * (uniqueCodesCount >= LargeInputSizeThreshold ? MaxLargeBucketTableMultiplier : MaxSmallBucketTableMultiplier);

        int maxPrimeIndexExclusive = minPrimeIndexInclusive;
        while ((uint)maxPrimeIndexExclusive < (uint)primes.Length && maxNumBuckets > primes[maxPrimeIndexExclusive])
            maxPrimeIndexExclusive++;

        if (maxPrimeIndexExclusive < primes.Length)
        {
            Debug.Assert(maxPrimeIndexExclusive != 0);
            maxNumBuckets = primes[maxPrimeIndexExclusive - 1];
        }

        const int BitsPerInt32 = 32;
        int[] seenBuckets = ArrayPool<int>.Shared.Rent((maxNumBuckets / BitsPerInt32) + 1);

        int bestNumBuckets = maxNumBuckets;
        int bestNumCollisions = uniqueCodesCount;
        int numBuckets, numCollisions;

        for (int primeIndex = minPrimeIndexInclusive; primeIndex < maxPrimeIndexExclusive; primeIndex++)
        {
            numBuckets = primes[primeIndex];
            Array.Clear(seenBuckets, 0, Math.Min(numBuckets, seenBuckets.Length));

            numCollisions = 0;

            if (codes is not null && uniqueCodesCount != hashCodes.Length)
            {
                foreach (int code in codes)
                    if (!IsBucketFirstVisit(code))
                        break;
            }
            else
            {
                foreach (int code in hashCodes)
                    if (!IsBucketFirstVisit(code))
                        break;
            }

            if (numCollisions < bestNumCollisions)
            {
                bestNumBuckets = numBuckets;

                if (numCollisions / (double)uniqueCodesCount <= AcceptableCollisionRate)
                    break;

                bestNumCollisions = numCollisions;
            }
        }

        ArrayPool<int>.Shared.Return(seenBuckets);

        return bestNumBuckets;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsBucketFirstVisit(int code)
        {
            uint bucketNum = (uint)code % (uint)numBuckets;
            if ((seenBuckets[bucketNum / BitsPerInt32] & (1 << (int)bucketNum)) != 0)
            {
                numCollisions++;
                if (numCollisions >= bestNumCollisions)
                    return false;
            }
            else
                seenBuckets[bucketNum / BitsPerInt32] |= 1 << (int)bucketNum;

            return true;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Bucket(int StartIndex, int EndIndex);

    private static void RenderBucket(StringBuilder sb, Bucket obj) => sb.Append("        new Bucket(").Append(obj.StartIndex).Append(", ").Append(obj.EndIndex).Append(')');
    private static void RenderItem(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
    private static void RenderHashCode(StringBuilder sb, int obj) => sb.Append(obj);
}