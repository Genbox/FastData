using System.Runtime.CompilerServices;

namespace Genbox.FastData.Benchmarks.Benchmarks;

public class GetHashCodeBenchmarks
{
    private readonly double _value = 12.5; //Don't make const (it will make the compiler optimize further). We need to simulate real-lfie.

    [Benchmark]
    public uint DoubleHashCode()
    {
        uint val = 0;

        for (int i = 0; i < 1000; i++)
            val += (uint)_value.GetHashCode();

        return val;
    }

    [Benchmark]
    public uint DoubleHashCodeCustom()
    {
        uint val = 0;

        for (int i = 0; i < 1000; i++)
            val += GetHashCode(_value);

        return val;
    }

    [Benchmark]
    public ulong DoubleHashCodeCustom64()
    {
        ulong val = 0;

        for (int i = 0; i < 1000; i++)
            val += GetHashCode64(_value);

        return val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetHashCode(double value)
    {
        ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
            bits &= 0x7FF0_0000_0000_0000;

        return unchecked((uint)bits) ^ (uint)(bits >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetHashCode64(double value)
    {
        ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
            bits &= 0x7FF0_0000_0000_0000;

        return bits;
    }
}