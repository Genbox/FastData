using System.Globalization;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Analysis.BruteForce;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct BFHashSpec(HashFunction HashFunction, StringSegment[] Segments) : IHashSpec
{
    public Func<string, uint> GetFunction()
    {
        StringSegment seg = Segments[0];

        return HashFunction switch
        {
            HashFunction.Frozen => s => FrozenHash.ComputeHash(seg.GetSpan(s)),
            HashFunction.WyHash => s => WyHash.ComputeHash(seg.GetSpan(s)),
            HashFunction.XxHash => s => XxHash.ComputeHash(seg.GetSpan(s)),
            _ => throw new InvalidOperationException("Unsupported hash function " + HashFunction)
        };
    }

    public string Construct()
    {
        return $$"""
                 public static int Hash(ReadOnlySpan<char> str)
                 {
                     return {{HashFunction}}.ComputeHash({{GetSlice(Segments[0])}});
                 }
                 """;
    }

    private static string GetSlice(StringSegment segment)
    {
        if (segment.Alignment == Alignment.Left)
        {
            if (segment.Offset == 0 && segment.Length == -1)
                return "str";
            if (segment.Offset != 0 && segment.Length != -1)
                return $"str.Slice({segment.Offset.ToString(NumberFormatInfo.InvariantInfo)})";

            return $"str.Slice({segment.Offset}, {segment.Length})";
        }

        if (segment.Alignment == Alignment.Right)
        {
            if (segment.Offset == 0 && segment.Length == -1)
                return "str";
            if (segment.Offset != 0 && segment.Length != -1)
                return $"str.Slice(str.Length - {segment.Offset.ToString(NumberFormatInfo.InvariantInfo)})";

            return $"str.Slice(str.Length - {segment.Offset} - {segment.Length}, {segment.Length})";
        }

        throw new InvalidOperationException("Invalid alignment: " + segment.Alignment);
    }
}