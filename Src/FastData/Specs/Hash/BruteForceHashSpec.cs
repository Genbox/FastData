using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
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
            HashFunction.DJB2Hash => obj =>
            {
                string str = (string)obj;
                ref char ptr = ref MemoryMarshal.GetReference(SegmentHelper.GetSpan(seg, str));
                return DJB2Hash.ComputeHash(ref ptr, str.Length);
            },
            HashFunction.XxHash => obj => XxHash.ComputeHash(SegmentHelper.GetSpan(seg, (string)obj)),
            _ => throw new InvalidOperationException("Unsupported hash function " + HashFunction)
        };
    }

    public EqualFunc GetEqualFunction() => static (a, b) => ((string)a).Equals((string)b, StringComparison.Ordinal);
}