using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal struct AsciiMap()
{
    public ulong Low = 0;
    public ulong High = 0;
    public char Min = char.MaxValue;
    public char Max = char.MinValue;

    public int BitCount => BitOperations.PopCount(Low) + BitOperations.PopCount(High);
    // Density is calculated over the observed value range (Min..Max), not the full ASCII range.
    public float Density
    {
        get
        {
            if (BitCount == 0 || Max < Min)
                return 0f;

            uint range = (uint)(Max - Min + 1);
            return BitCount / (float)range;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(char value)
    {
        if (value > 0x7F)
            return;

        Min = value < Min ? value : Min;
        Max = value > Max ? value : Max;

        if (value < 64)
            Low |= 1UL << value;
        else
            High |= 1UL << (value - 64);
    }
}