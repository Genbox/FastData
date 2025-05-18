using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Specs.Hash;

public sealed record DefaultStringHash(bool UseUTF16Encoding) : IStringHash
{
    public HashFunc<string> GetHashFunction() => str =>
    {
        if (UseUTF16Encoding)
            return DJB2Hash.ComputeHash64(ref MemoryMarshal.GetReference(str.AsSpan()), str.Length);

        byte[] bytes = Encoding.UTF8.GetBytes(str);
        return DJB2Hash.ComputeHash64(ref bytes[0], bytes.Length);
    };

    public EqualFunc<string> GetEqualFunction() => static (a, b) => a.Equals(b, StringComparison.Ordinal);
}