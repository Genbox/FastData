using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Hashes;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Specs.Hash;

[StructLayout(LayoutKind.Auto)]
public readonly record struct BruteForceHashSpec(StringSegment Segment) : IHashSpec
{
    public HashFunc<string> GetHashFunction()
    {
        StringSegment segment = Segment;

        return str =>
        {
            ref char ptr = ref MemoryMarshal.GetReference(SegmentHelper.GetSpan(segment, str));
            return DJB2Hash.ComputeHash(ref ptr, str.Length);
        };
    }

    public EqualFunc<string> GetEqualFunction() => static (a, b) => a.Equals(b, StringComparison.Ordinal);
}