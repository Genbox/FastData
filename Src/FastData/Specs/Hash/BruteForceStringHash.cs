using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Hashes;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Specs.Misc;

namespace Genbox.FastData.Specs.Hash;

public sealed record BruteForceStringHash(StringSegment Segment) : IStringHash
{
    public HashFunc<string> GetHashFunction() => str =>
    {
        ref char ptr = ref MemoryMarshal.GetReference(SegmentHelper.GetSpan(Segment, str));
        return DJB2Hash.ComputeHash(ref ptr, str.Length);
    };

    public EqualFunc<string> GetEqualFunction() => static (a, b) => a.Equals(b, StringComparison.Ordinal);
}