using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
// ReSharper disable All

namespace Genbox.FastData.Benchmarks.Benchmarks;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class GetHashCodeBenchmarks
{
    //Don't make const (it will make the compiler optimize further). We need to simulate real-life.
    private readonly sbyte _i8 = 12;
    private readonly byte _u8 = 12;
    private readonly short _i16 = 12;
    private readonly ushort _u16 = 12;
    private readonly int _i32 = 12;
    private readonly uint _u32 = 12;
    private readonly long _i64 = 12;
    private readonly ulong _u64 = 12;
    private readonly float _f32 = 12;
    private readonly double _f64 = 12;

    private const int _iterations = 1000;

    [BenchmarkCategory("I8"), Benchmark(Baseline = true)]public uint I8HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_i8.GetHashCode(); return val; }
    [BenchmarkCategory("I8"), Benchmark]public ulong I8FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_i8; return val; }

    [BenchmarkCategory("U8"), Benchmark(Baseline = true)]public uint U8HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_u8.GetHashCode(); return val; }
    [BenchmarkCategory("U8"), Benchmark]public ulong U8FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_u8; return val; }

    [BenchmarkCategory("I16"), Benchmark(Baseline = true)]public uint I16HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_i16.GetHashCode(); return val; }
    [BenchmarkCategory("I16"), Benchmark]public ulong I16FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_i16; return val; }

    [BenchmarkCategory("U16"), Benchmark(Baseline = true)]public uint U16HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_u16.GetHashCode(); return val; }
    [BenchmarkCategory("U16"), Benchmark]public ulong U16FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_u16; return val; }

    [BenchmarkCategory("I32"), Benchmark(Baseline = true)]public uint I32HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_i32.GetHashCode(); return val; }
    [BenchmarkCategory("I32"), Benchmark]public ulong I32FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_i32; return val; }

    [BenchmarkCategory("U32"), Benchmark(Baseline = true)]public uint U32HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_u32.GetHashCode(); return val; }
    [BenchmarkCategory("U32"), Benchmark]public ulong U32FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_u32; return val; }

    [BenchmarkCategory("I64"), Benchmark(Baseline = true)]public uint I64HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_i64.GetHashCode(); return val; }
    [BenchmarkCategory("I64"), Benchmark]public ulong I64FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_i64; return val; }

    [BenchmarkCategory("U64"), Benchmark(Baseline = true)]public uint U64HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_u64.GetHashCode(); return val; }
    [BenchmarkCategory("U64"), Benchmark]public ulong U64FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += (ulong)_u64; return val; }

    [BenchmarkCategory("F32"), Benchmark(Baseline = true)]public uint F32HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_f32.GetHashCode(); return val; }
    [BenchmarkCategory("F32"), Benchmark]public ulong F32FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += HashF32(_f32); return val; }
    [BenchmarkCategory("F32"), Benchmark]public ulong F32FastHashAlt() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += HashF32Alt(_f32); return val; }

    [BenchmarkCategory("F64"), Benchmark(Baseline = true)]public uint F64HashCode() { uint val = 0; for (int i = 0; i < _iterations; i++) val += (uint)_f64.GetHashCode(); return val; }
    [BenchmarkCategory("F64"), Benchmark]public ulong F64FastHash() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += HashF64(_f64); return val; }
    [BenchmarkCategory("F64"), Benchmark]public ulong F64FastHashAlt() { ulong val = 0; for (int i = 0; i < _iterations; i++) val += HashF64Alt(_f64); return val; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashF32(float value)
    {
        uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000)) >= 0x7FF0_0000)
            bits &= 0x7FF0_0000;

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashF32Alt(float value) => Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashF64(double value)
    {
        ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
            bits &= 0x7FF0_0000_0000_0000;

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashF64Alt(double value) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));
}