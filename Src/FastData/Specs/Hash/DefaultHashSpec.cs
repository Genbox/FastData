using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Specs.Hash;

public sealed class DefaultHashSpec(bool useUTF16Encoding) : IHashSpec
{
    public HashFunc GetHashFunction() => obj =>
    {
        if (obj is string str)
        {
            if (useUTF16Encoding)
                return DJB2Hash.ComputeHash(ref MemoryMarshal.GetReference(str.AsSpan()), str.Length);

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return DJB2Hash.ComputeHash(ref bytes[0], bytes.Length);
        }

        uint hash = obj switch
        {
            char val => val,
            sbyte val => (uint)val,
            byte val => val,
            short val => (uint)val,
            ushort val => val,
            int val => (uint)val,
            uint val => val,
            float val => GetHashCode(val),
            double val => GetHashCode(val),
            _ => (uint)obj.GetHashCode()
        };

        return hash;
    };

    private static uint GetHashCode(float value)
    {
        uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000)) >= 0x7F80_0000)
            bits &= 0x7F80_0000;

        return bits;
    }

    private static uint GetHashCode(double value)
    {
        ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
            bits &= 0x7FF0_0000_0000_0000;

        return unchecked((uint)bits) ^ (uint)(bits >> 32);
    }

    public EqualFunc GetEqualFunction() => static (a, b) => ((string)a).Equals((string)b, StringComparison.Ordinal);
}