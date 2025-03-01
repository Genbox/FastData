using System.Globalization;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Analysis.BruteForce;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct BruteForceHashSpec(HashFunction HashFunction, StringSegment[] Segments) : IHashSpec
{
    public Func<string, uint> GetFunction()
    {
        StringSegment seg = Segments[0];

        return HashFunction switch
        {
            HashFunction.DJB2Hash => s => DJB2Hash.ComputeHash(seg.GetSpan(s)),
            HashFunction.XxHash => s => XxHash.ComputeHash(seg.GetSpan(s)),
            _ => throw new InvalidOperationException("Unsupported hash function " + HashFunction)
        };
    }

    public string GetSource()
        => $$"""
                 [MethodImpl(MethodImplOptions.AggressiveInlining)]
                 public static uint Hash(string str)
                 {
                     return Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions.{{HashFunction}}.ComputeHash({{GetSlice(Segments[0])}});
                 }
             """;

    private static string GetSlice(StringSegment segment)
    {
        if (segment.Alignment == Alignment.Left)
        {
            if (segment.Offset == 0 && segment.Length == -1)
                return "str";
            if (segment.Offset != 0 && segment.Length == -1)
                return $"str.AsSpan({segment.Offset.ToString(NumberFormatInfo.InvariantInfo)})";

            return $"str.AsSpan({segment.Offset}, {segment.Length})";
        }

        if (segment.Alignment == Alignment.Right)
        {
            if (segment.Offset == 0 && segment.Length == -1)
                return "str";
            if (segment.Offset != 0 && segment.Length == -1)
                return $"str.AsSpan(0, str.Length - {segment.Offset} - {segment.Length})";

            return $"str.AsSpan(str.Length - {segment.Offset} - {segment.Length}, {segment.Length})";
        }

        throw new InvalidOperationException("Invalid alignment: " + segment.Alignment);
    }
}