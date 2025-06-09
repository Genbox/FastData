using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Order;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.Benchmarks.Benchmarks;

/// <summary>Benchmarks the difference between checking string/byte[]</summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class StringVsBytesBenchmarks
{
    //Create 100x 4 character strings. They are all ASCII
    private static readonly string[] _query1 = Enumerable.Range(1, 100).Select(_ => TestHelper.GenerateRandomString(Random.Shared, 4)).ToArray();
    private static readonly byte[][] _query2 = _query1.Select(x => Encoding.ASCII.GetBytes(x)).ToArray();
    private static readonly int[] _query3 = _query2.Select(x => BitConverter.ToInt32(x)).ToArray();

    private readonly HashSet<string> _hashSet1 = _query1.ToHashSet(StringComparer.Ordinal);
    private readonly HashSet<byte[]> _hashSet2 = _query2.ToHashSet(new ByteArrayComparer());
    private readonly HashSet<byte[]> _hashSet3 = _query2.ToHashSet(new UnsafeByteArrayComparer());
    private readonly HashSet<int> _hashSet4 = _query3.ToHashSet();

    [Benchmark(Baseline = true)]
    public void String()
    {
        for (int i = 0; i < _query1.Length; i++)
        {
            if (!_hashSet1.Contains(_query1[i]))
                throw new InvalidOperationException();
        }
    }

    [Benchmark]
    public void ByteArray()
    {
        for (int i = 0; i < _query2.Length; i++)
        {
            if (!_hashSet2.Contains(_query2[i]))
                throw new InvalidOperationException();
        }
    }

    [Benchmark]
    public void ByteArrayUnsafe()
    {
        for (int i = 0; i < _query2.Length; i++)
        {
            if (!_hashSet3.Contains(_query2[i]))
                throw new InvalidOperationException();
        }
    }

    [Benchmark]
    public void Integer()
    {
        for (int i = 0; i < _query3.Length; i++)
        {
            if (!_hashSet4.Contains(_query3[i]))
                throw new InvalidOperationException();
        }
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null || y == null || x.Length != y.Length)
                return false;

            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[]? obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (byte b in obj)
                {
                    hash = hash * 31 + b;
                }
                return hash;
            }
        }
    }

    private sealed class UnsafeByteArrayComparer : IEqualityComparer<byte[]>
    {
        private const uint FNV_OFFSET = 2166136261;
        private const uint FNV_PRIME = 16777619;

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null || y == null || x.Length != y.Length)
                return false;

            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            unchecked
            {
                uint hash = FNV_OFFSET;
                int length = obj.Length;
                ref byte p = ref obj[0];

                int offset = 0;
                int count = length >> 3;
                for (int i = 0; i < count; i++, offset += 8)
                {
                    ulong v = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref p, offset));
                    hash = (hash ^ (uint)v) * FNV_PRIME;
                    hash = (hash ^ (uint)(v >> 32)) * FNV_PRIME;
                }

                int rem = length & 7;
                for (int i = 0; i < rem; i++)
                {
                    byte b = Unsafe.Add(ref p, offset + i);
                    hash = (hash ^ b) * FNV_PRIME;
                }

                return (int)hash;
            }
        }
    }
}