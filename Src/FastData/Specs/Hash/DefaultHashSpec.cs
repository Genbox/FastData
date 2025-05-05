using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Specs.Hash;

public sealed class DefaultHashSpec(bool useUTF16Encoding) : IHashSpec
{
    public HashFunc<string> GetHashFunction() => str =>
    {
        if (useUTF16Encoding)
            return DJB2Hash.ComputeHash(ref MemoryMarshal.GetReference(str.AsSpan()), str.Length);

        byte[] bytes = Encoding.UTF8.GetBytes(str);
        return DJB2Hash.ComputeHash(ref bytes[0], bytes.Length);
    };

    public EqualFunc<string> GetEqualFunction() => static (a, b) => a.Equals(b, StringComparison.Ordinal);
}