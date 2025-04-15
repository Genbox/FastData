using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Hashes;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Specs.Hash;

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("Naming", "CA1721:Property names should not match get methods")]
public readonly record struct BruteForceHashSpec(HashFunction HashFunction, StringSegment[] Segments) : IHashSpec
{
    public HashFunc GetHashFunction()
    {
        StringSegment seg = Segments[0];

        return HashFunction switch
        {
            HashFunction.DJB2Hash => (obj, seed) => DJB2Hash.ComputeHash(SegmentHelper.GetSpan(seg, (string)obj), seed != 0 ? seed : DJB2Hash.Seed),
            HashFunction.XxHash => (obj, seed) => XxHash.ComputeHash(SegmentHelper.GetSpan(seg, (string)obj), seed != 0 ? seed : XxHash.PRIME64_5),
            _ => throw new InvalidOperationException("Unsupported hash function " + HashFunction)
        };
    }

    public EqualFunc GetEqualFunction() => static (a, b) => ((string)a).Equals((string)b, StringComparison.Ordinal);
}