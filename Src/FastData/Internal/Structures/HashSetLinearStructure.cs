using System.Diagnostics;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetLinearStructure<T>(HashData hashData) : IStructure<T, HashSetLinearContext<T>>
{
    //TODO: Either implement a bitmap for seen buckets everywhere or don't use bitmaps for simplicity

    public HashSetLinearContext<T> Create(ref ReadOnlySpan<T> data)
    {
        ulong[] hashCodes = hashData.HashCodes;
        uint numBuckets = CalcNumBuckets(hashCodes, hashData.HashCodesUnique);
        int[] bucketStarts = new int[numBuckets];

        for (int i = 0; i < bucketStarts.Length; i++)
        {
            bucketStarts[i] = -1;
        }

        int[] nexts = new int[hashCodes.Length];

        for (int i = 0; i < hashCodes.Length; i++)
        {
            ulong hashCode = hashCodes[i];
            uint bucketNum = (uint)(hashCode % numBuckets);

            ref int bucketStart = ref bucketStarts[bucketNum];
            nexts[i] = bucketStart;
            bucketStart = i;
        }

        ulong[] finalCodes = new ulong[hashCodes.Length];
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
                ref ulong hashCode = ref hashCodes[index];
                finalCodes[count] = hashCode;
                hashCode = (ulong)count;
                count++;
                bucketCount++;

                index = nexts[index];
            }

            finalBuckets[bucketNum] = new HashSetBucket(bucketStart, bucketStart + bucketCount - 1);
        }

        T[] newData = new T[data.Length];

        for (int srcIndex = 0; srcIndex < hashCodes.Length; srcIndex++)
        {
            ulong destIndex = hashCodes[srcIndex];
            newData[destIndex] = data[srcIndex];
        }

        return new HashSetLinearContext<T>(newData, finalBuckets, finalCodes);
    }

    private static uint CalcNumBuckets(ulong[] hashCodes, bool hashCodesUnique)
    {
        //Note: this code starts with a sane capacity factor for how many buckets are needed.
        //      it then increases the bucket capacity with the next prime number until it reaches less than 5% collisions
        //      it does this using a bitmap of buckets seen. It also uses unique hash codes to avoid duplicates counting toward collisions

        const double AcceptableCollisionRate = 0.05;
        const int LargeInputSizeThreshold = 1000;
        const int MaxSmallBucketTableMultiplier = 16;
        const uint MaxLargeBucketTableMultiplier = 3;

        ICollection<ulong> codes = hashCodes;

        if (!hashCodesUnique)
        {
            HashSet<ulong> uniqCodes = new HashSet<ulong>();
            foreach (ulong hashCode in hashCodes)
                uniqCodes.Add(hashCode);

            codes = uniqCodes;
        }

        uint uniqueCodesCount = (uint)codes.Count;
        uint minNumBuckets = uniqueCodesCount * 2;

        uint[] primes = MathHelper.Primes;
        uint minPrimeIndexInclusive = 0;
        while (minPrimeIndexInclusive < (uint)primes.Length && minNumBuckets > primes[minPrimeIndexInclusive])
        {
            minPrimeIndexInclusive++;
        }

        if (minPrimeIndexInclusive >= primes.Length)
            return MathHelper.GetPrime(uniqueCodesCount);

        uint maxNumBuckets = uniqueCodesCount * (uniqueCodesCount >= LargeInputSizeThreshold ? MaxLargeBucketTableMultiplier : MaxSmallBucketTableMultiplier);

        uint maxPrimeIndexExclusive = minPrimeIndexInclusive;
        while (maxPrimeIndexExclusive < (uint)primes.Length && maxNumBuckets > primes[maxPrimeIndexExclusive])
        {
            maxPrimeIndexExclusive++;
        }

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

            foreach (ulong code in codes)
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

        bool IsBucketFirstVisit(ulong code)
        {
            int bucketNum = (int)(code % numBuckets);
            if ((seenBuckets[bucketNum / 32] & (1 << bucketNum)) != 0)
            {
                numCollisions++;
                if (numCollisions >= bestNumCollisions)
                    return false;
            }
            else
                seenBuckets[bucketNum / 32] |= 1 << bucketNum;

            return true;
        }
    }
}