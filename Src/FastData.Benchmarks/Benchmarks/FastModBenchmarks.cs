using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Genbox.FastData.Generators.Helpers;

namespace Genbox.FastData.Benchmarks.Benchmarks;

/// <summary>Benchmarks the difference between normal modulus and FastMod</summary>
[SuppressMessage("ReSharper", "ConvertToConstant.Local")]
public class FastModBenchmarks
{
    private const ushort _modConst = 1024;
    private readonly ushort _mod = 1024; //Note: Do not make this const!

    private readonly ulong _mult = MathHelper.GetFastModMultiplier(int.MaxValue);
    private readonly uint _mult2 = GetFastModMultiplierSmall(3000);
    private readonly uint _value = 3_000_000_000; //Note: Do not make this const!

    [Benchmark]
    public uint Const() => _value % 1024;

    [Benchmark]
    public uint ConstAdd() => _value & 1023;

    [Benchmark]
    public long Var() => _value % _mod;

    [Benchmark]
    public long VarConst() => _value % _modConst;

    [Benchmark]
    public long VarAdd() => _value & 1023;

    [Benchmark]
    public ulong FastMod() => MathHelper.FastMod(_value, 1024, _mult);

    [Benchmark]
    public ulong FastModSmall() => FastModSmall(3000, 1024, _mult2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetFastModMultiplierSmall(uint divisor) => unchecked((uint.MaxValue / divisor) + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FastModSmall(ushort value, ushort divisor, uint multiplier) => ((ulong)(multiplier * value) * divisor) >> 32;
}