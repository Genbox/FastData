using System.Diagnostics;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Contexts.Misc;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetLinearStructure : IHashStructure
{
    //TODO: Either implement a bitmap for seen buckets everywhere or don't use bitmaps for simplicity

    public bool TryCreate(object[] data, HashFunc hash, out IContext? context)
    {
        uint[] hashCodes = new uint[data.Length];
        for (int i = 0; i < data.Length; i++)
            hashCodes[i] = hash(data[i]);

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
        HashSetBucket[] finalBuckets = new HashSetBucket[bucketStarts.Length];
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

            finalBuckets[bucketNum] = new HashSetBucket(bucketStart, (bucketStart + bucketCount) - 1);
        }

        object[] newData = new object[data.Length];

        for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
        {
            uint destIndex = hashCodes[srcIndex];
            newData[destIndex] = data[srcIndex];
        }

        context = new HashSetLinearContext(newData, finalBuckets, finalCodes);
        return true;
    }

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
            return MathHelper.GetPrime(uniqueCodesCount);

        uint maxNumBuckets = uniqueCodesCount * (uniqueCodesCount >= LargeInputSizeThreshold ? MaxLargeBucketTableMultiplier : MaxSmallBucketTableMultiplier);

        uint maxPrimeIndexExclusive = minPrimeIndexInclusive;
        while (maxPrimeIndexExclusive < (uint)primes.Length && maxNumBuckets > primes[maxPrimeIndexExclusive])
            maxPrimeIndexExclusive++;

        if (maxPrimeIndexExclusive < primes.Length)
        {
            Debug.Assert(maxPrimeIndexExclusive != 0);
            maxNumBuckets = primes[maxPrimeIndexExclusive - 1];
        }

        int[] seenBuckets = new int[(maxNumBuckets / 32) + 1];

        uint bestNumBuckets = maxNumBuckets;
        uint bestNumCollisions = uniqueCodesCount;
        uint numBuckets, numCollisions;

        for (uint primeIndex = minPrimeIndexInclusive; primeIndex < maxPrimeIndexExclusive; primeIndex++)
        {
            numBuckets = primes[primeIndex];
            Array.Clear(seenBuckets, 0, (int)Math.Min(numBuckets, seenBuckets.Length));

            numCollisions = 0;

            foreach (uint code in codes)
            {
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

        return bestNumBuckets;

        bool IsBucketFirstVisit(uint code)
        {
            uint bucketNum = code % numBuckets;
            if ((seenBuckets[bucketNum / 32] & (1 << (int)bucketNum)) != 0)
            {
                numCollisions++;
                if (numCollisions >= bestNumCollisions)
                    return false;
            }
            else
                seenBuckets[bucketNum / 32] |= 1 << (int)bucketNum;

            return true;
        }
    }
}