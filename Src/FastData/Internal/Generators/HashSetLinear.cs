using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class HashSetLinear(FastDataConfig config, GeneratorContext context) : HashSetCode.IHashSetBase
{
    private Bucket[] _buckets;
    private uint[] _hashCodes;
    private object[] _items;

    public void Create(Func<object, uint> hash)
    {
        uint[] hashCodes = new uint[config.Data.Length];
        for (int i = 0; i < config.Data.Length; i++)
            hashCodes[i] = hash(config.Data[i]);

        uint numBuckets = CalcNumBuckets(hashCodes);
        int[] bucketStarts = new int[numBuckets];

        for (int i = 0; i < bucketStarts.Length; i++)
            bucketStarts[i] = -1;

        int[] nexts = new int[hashCodes.Length];

        for (int i = 0; i < hashCodes.Length; i++)
        {
            uint hashCode = hashCodes[i];
            uint bucketNum = hashCode % numBuckets;

            ref int bucketStart = ref bucketStarts[bucketNum];
            nexts[i] = bucketStart;
            bucketStart = i;
        }

        uint[] finalCodes = new uint[hashCodes.Length];
        Bucket[] finalBuckets = new Bucket[bucketStarts.Length];
        int count = 0;
        for (int bucketNum = 0; bucketNum < finalBuckets.Length; bucketNum++)
        {
            int bucketStart = bucketStarts[bucketNum];
            if (bucketStart < 0)
                continue;

            int bucketCount = 0;
            int index = bucketStart;
            bucketStart = count;
            while (index >= 0)
            {
                ref uint hashCode = ref hashCodes[index];
                finalCodes[count] = hashCode;
                hashCode = (uint)count;
                count++;
                bucketCount++;

                index = nexts[index];
            }

            finalBuckets[bucketNum] = new Bucket(bucketStart, (bucketStart + bucketCount) - 1);
        }

        _hashCodes = finalCodes;
        _buckets = finalBuckets;
        _items = new object[config.Data.Length];

        for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
        {
            uint destIndex = hashCodes[srcIndex];
            _items[destIndex] = config.Data[srcIndex];
        }
    }

    public string Generate(IHashSpec? spec) =>
        $$"""
              private{{GetModifier(config.ClassType)}} readonly Bucket[] _buckets = {
          {{JoinValues(_buckets, RenderBucket, ",\n")}}
              };

              private{{GetModifier(config.ClassType)}} readonly {{config.DataType}}[] _items = {
          {{JoinValues(_items, RenderItem, ",\n")}}
              };

              private{{GetModifier(config.ClassType)}} readonly uint[] _hashCodes = { {{JoinValues(_hashCodes, RenderHashCode)}} };

              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  uint hashCode = {{(spec != null ? "Hash(value)" : GetHashFunction32(config.DataType, "value"))}};
                  ref Bucket b = ref _buckets[{{GetModFunction("hashCode", (uint)_buckets.Length)}}];

                  int index = b.StartIndex;
                  int endIndex = b.EndIndex;

                  while (index <= endIndex)
                  {
                      if (hashCode == _hashCodes[index] && {{GetEqualFunction(config.DataType, "value", "_items[index]")}})
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

    //TODO: Start and End index can be smaller if there are fewer items
    //TODO: Either implement a bitmap for seen buckets everywhere or don't use bitmaps for simplicity

    private static uint CalcNumBuckets(ReadOnlySpan<uint> hashCodes)
    {
        //Note: this code starts with a sane capacity factor for how many buckets are needed.
        //      it then increase the bucket capacity with the next prime number until it reaches less than 5% collisions
        //      it does this using a bitmap of buckets seen. It also uses unique hash codes to avoid duplicates counting toward collisions

        const double AcceptableCollisionRate = 0.05;
        const int LargeInputSizeThreshold = 1000;
        const int MaxSmallBucketTableMultiplier = 16;
        const uint MaxLargeBucketTableMultiplier = 3;

        HashSet<uint> codes = new HashSet<uint>();

        foreach (uint hashCode in hashCodes)
            codes.Add(hashCode);

        uint uniqueCodesCount = (uint)codes.Count;
        uint minNumBuckets = uniqueCodesCount * 2;

        uint[] primes = MathHelper.Primes;
        uint minPrimeIndexInclusive = 0;
        while (minPrimeIndexInclusive < (uint)primes.Length && minNumBuckets > primes[minPrimeIndexInclusive])
            minPrimeIndexInclusive++;

        if (minPrimeIndexInclusive >= primes.Length)
            return (uint)MathHelper.GetPrime((int)uniqueCodesCount);

        uint maxNumBuckets = uniqueCodesCount * (uniqueCodesCount >= LargeInputSizeThreshold ? MaxLargeBucketTableMultiplier : MaxSmallBucketTableMultiplier);

        uint maxPrimeIndexExclusive = minPrimeIndexInclusive;
        while (maxPrimeIndexExclusive < (uint)primes.Length && maxNumBuckets > primes[maxPrimeIndexExclusive])
            maxPrimeIndexExclusive++;

        if (maxPrimeIndexExclusive < primes.Length)
        {
            Debug.Assert(maxPrimeIndexExclusive != 0);
            maxNumBuckets = primes[maxPrimeIndexExclusive - 1];
        }

        const int BitsPerInt32 = 32;
        int[] seenBuckets = new int[(maxNumBuckets / BitsPerInt32) + 1];

        uint bestNumBuckets = maxNumBuckets;
        uint bestNumCollisions = uniqueCodesCount;
        uint numBuckets, numCollisions;

        for (uint primeIndex = minPrimeIndexInclusive; primeIndex < maxPrimeIndexExclusive; primeIndex++)
        {
            numBuckets = primes[primeIndex];
            Array.Clear(seenBuckets, 0, (int)Math.Min(numBuckets, seenBuckets.Length));

            numCollisions = 0;

            foreach (uint code in codes)
                if (!IsBucketFirstVisit(code))
                    break;

            if (numCollisions < bestNumCollisions)
            {
                bestNumBuckets = numBuckets;

                if (numCollisions / (double)uniqueCodesCount <= AcceptableCollisionRate)
                    break;

                bestNumCollisions = numCollisions;
            }
        }

        return bestNumBuckets;

        bool IsBucketFirstVisit(uint code)
        {
            uint bucketNum = code % numBuckets;
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
    private static void RenderHashCode(StringBuilder sb, uint obj) => sb.Append(obj);
}