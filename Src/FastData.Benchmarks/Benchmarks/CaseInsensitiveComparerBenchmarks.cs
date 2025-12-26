using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Genbox.FastData.Benchmarks.Benchmarks;

public class CaseInsensitiveComparerBenchmarks
{
    private static string _target = "thisismystringwithcasing";
    private static string _myString = "ThisIsMyStringWithCasing";
    private const int TargetLength = 24;

    [Benchmark]public bool WithKeyNormalization() => _myString.ToLowerInvariant() == _target;
    [Benchmark]public bool WithComparer() => StringComparer.OrdinalIgnoreCase.Equals(_myString, _target);
    [Benchmark]public bool WithScalar() => WithScalar(_myString, _target);
    [Benchmark]public bool WithUnsafe() => WithUnsafe(_myString, _target);
    [Benchmark]public bool WithSwar() => WithSwar(_myString, _target);
    [Benchmark]public bool WithSimdAvx2() => WithSimdAvx2(_myString, _target);
    [Benchmark]public bool WithSimdSse2() => WithSimdSse2(_myString, _target);
    [Benchmark]public bool WithTable() => WithTable(_myString, _target);
    [Benchmark]public bool OptimizedCaseSensitive() => OptimizedCaseSensitive(_myString, _myString);

    private static bool WithScalar(string s1, string s2)
    {
        ref char s1Ref = ref MemoryMarshal.GetReference(s1.AsSpan());
        ref char s2Ref = ref MemoryMarshal.GetReference(s2.AsSpan());

        for (int i = 0; i < TargetLength; i++)
        {
            uint c1 = Unsafe.Add(ref s1Ref, i);
            if (c1 - 'A' <= 'Z' - 'A')
                c1 |= 0x20;

            char c2 = Unsafe.Add(ref s2Ref, i);
            if (c1 != c2)
                return false;
        }

        return true;
    }

    private static unsafe bool WithUnsafe(string s1, string s2)
    {
        fixed (char* p1 = s1)
        fixed (char* p2 = s2)
        {
            char* a = p1;
            char* b = p2;
            char* end = a + TargetLength;

            while (a != end)
            {
                uint c1 = *a;
                if (c1 - 'A' <= 'Z' - 'A')
                    c1 |= 0x20;

                if (c1 != *b)
                    return false;

                a++;
                b++;
            }
        }

        return true;
    }

    private static bool WithSwar(string s1, string s2)
    {
        ref char s1Ref = ref MemoryMarshal.GetReference(s1.AsSpan());
        ref char s2Ref = ref MemoryMarshal.GetReference(s2.AsSpan());
        ref byte s1ByteRef = ref Unsafe.As<char, byte>(ref s1Ref);
        ref byte s2ByteRef = ref Unsafe.As<char, byte>(ref s2Ref);

        const int byteLength = TargetLength * 2;

        for (int i = 0; i < byteLength; i += sizeof(ulong))
        {
            ulong v = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s1ByteRef, i));
            ulong t = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s2ByteRef, i));
            ulong folded = FoldAsciiUppercase4x16(v);

            if (folded != t)
                return false;
        }

        return true;
    }

    private static bool WithSimdAvx2(string s1, string s2)
    {
        ref char s1Ref = ref MemoryMarshal.GetReference(s1.AsSpan());
        ref char s2Ref = ref MemoryMarshal.GetReference(s2.AsSpan());
        ref byte s1ByteRef = ref Unsafe.As<char, byte>(ref s1Ref);
        ref byte s2ByteRef = ref Unsafe.As<char, byte>(ref s2Ref);

        int i = 0;
        int length = TargetLength;

        for (; i <= length - 16; i += 16)
        {
            Vector256<ushort> v = Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.Add(ref s1ByteRef, i * 2));
            Vector256<ushort> t = Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.Add(ref s2ByteRef, i * 2));
            Vector256<ushort> lower = FoldAsciiUppercaseAvx2(v);
            Vector256<ushort> cmp = Avx2.CompareEqual(lower, t);

            if (Avx2.MoveMask(cmp.AsByte()) != -1)
                return false;
        }

        for (; i < length; i++)
        {
            uint c1 = Unsafe.Add(ref s1Ref, i);
            if (c1 - 'A' <= 'Z' - 'A')
                c1 |= 0x20;

            char c2 = Unsafe.Add(ref s2Ref, i);
            if (c1 != c2)
                return false;
        }

        return true;
    }

    private static bool WithSimdSse2(string s1, string s2)
    {
        ref char s1Ref = ref MemoryMarshal.GetReference(s1.AsSpan());
        ref char s2Ref = ref MemoryMarshal.GetReference(s2.AsSpan());
        ref byte s1ByteRef = ref Unsafe.As<char, byte>(ref s1Ref);
        ref byte s2ByteRef = ref Unsafe.As<char, byte>(ref s2Ref);

        int i = 0;

        for (; i <= TargetLength - 8; i += 8)
        {
            Vector128<ushort> v = Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.Add(ref s1ByteRef, i * 2));
            Vector128<ushort> t = Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.Add(ref s2ByteRef, i * 2));
            Vector128<ushort> lower = FoldAsciiUppercaseSse2(v);
            Vector128<ushort> cmp = Sse2.CompareEqual(lower, t);

            if (Sse2.MoveMask(cmp.AsByte()) != 0xFFFF)
                return false;
        }

        for (; i < TargetLength; i++)
        {
            uint c1 = Unsafe.Add(ref s1Ref, i);
            if (c1 - 'A' <= 'Z' - 'A')
                c1 |= 0x20;

            char c2 = Unsafe.Add(ref s2Ref, i);
            if (c1 != c2)
                return false;
        }

        return true;
    }

    private static bool WithTable(string s1, string s2)
    {
        ref char s1Ref = ref MemoryMarshal.GetReference(s1.AsSpan());
        ref char s2Ref = ref MemoryMarshal.GetReference(s2.AsSpan());
        ref byte tableRef = ref MemoryMarshal.GetReference(Downcase);

        for (int i = 0; i < TargetLength; i++)
        {
            byte c1 = Unsafe.Add(ref tableRef, Unsafe.Add(ref s1Ref, i));
            byte c2 = (byte)Unsafe.Add(ref s2Ref, i);

            if (c1 != c2)
                return false;
        }

        return true;
    }

    private static ulong FoldAsciiUppercase4x16(ulong value)
    {
        const ulong A = 0x0041004100410041UL;
        const ulong Z = 0x005A005A005A005AUL;
        const ulong HighBit = 0x8000800080008000UL;

        ulong a = value - A;
        ulong z = Z - value;
        ulong mask = (a | z) & HighBit;
        ulong add = (~mask & HighBit) >> 10;
        return value | add;
    }

    private static Vector256<ushort> FoldAsciiUppercaseAvx2(Vector256<ushort> value)
    {
        Vector256<short> a = Vector256.Create((short)'A');
        Vector256<short> z = Vector256.Create((short)'Z');
        Vector256<ushort> add = Vector256.Create((ushort)0x20);

        Vector256<short> v = value.AsInt16();
        Vector256<short> ltA = Avx2.CompareGreaterThan(a, v);
        Vector256<short> gtZ = Avx2.CompareGreaterThan(v, z);
        Vector256<short> outRange = Avx2.Or(ltA, gtZ);
        Vector256<ushort> mask = Avx2.AndNot(outRange.AsUInt16(), add);

        return Avx2.Or(value, mask);
    }

    private static Vector128<ushort> FoldAsciiUppercaseSse2(Vector128<ushort> value)
    {
        Vector128<short> a = Vector128.Create((short)'A');
        Vector128<short> z = Vector128.Create((short)'Z');
        Vector128<ushort> add = Vector128.Create((ushort)0x20);

        Vector128<short> v = value.AsInt16();
        Vector128<short> ltA = Sse2.CompareGreaterThan(a, v);
        Vector128<short> gtZ = Sse2.CompareGreaterThan(v, z);
        Vector128<short> outRange = Sse2.Or(ltA, gtZ);
        Vector128<ushort> mask = Sse2.AndNot(outRange.AsUInt16(), add);

        return Sse2.Or(value, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool OptimizedCaseSensitive(string s1, string s2)
    {
        int count = TargetLength;

        fixed (char* ap = s1)
        fixed (char* bp = s2)
        {
            char* a = ap;
            char* b = bp;

            if (*a != *b)
                return false;

            if (*(a + 1) != *(b + 1))
                goto DiffOffset1;

            count -= 2;
            a += 2;
            b += 2;

            while (count >= 12)
            {
                if (*(long*)a != *(long*)b) goto DiffOffset0;
                if (*(long*)(a + 4) != *(long*)(b + 4)) goto DiffOffset4;
                if (*(long*)(a + 8) != *(long*)(b + 8)) goto DiffOffset8;
                count -= 12;
                a += 12;
                b += 12;
            }

            while (count > 0)
            {
                if (*(int*)a != *(int*)b)
                    goto DiffNextInt;

                count -= 2;
                a += 2;
                b += 2;
            }

            return true;

            DiffOffset8:
            a += 4;
            b += 4;

            DiffOffset4:
            a += 4;
            b += 4;

            DiffOffset0:
            if (*(int*)a == *(int*)b)
            {
                a += 2;
                b += 2;
            }

            DiffNextInt:
            if (*a != *b)
                return false;

            DiffOffset1:
            return false;
        }
    }

    private static readonly byte[] Downcase =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
        15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
        30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44,
        45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
        60, 61, 62, 63, 64, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106,
        107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121,
        122, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104,
        105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119,
        120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134,
        135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149,
        150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164,
        165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179,
        180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194,
        195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209,
        210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224,
        225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239,
        240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254,
        255
    ];
}