using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Specs.Hash;

public sealed class DefaultHashSpec : IHashSpec
{
    private DefaultHashSpec() {}
    public static DefaultHashSpec Instance { get; } = new DefaultHashSpec();

    public HashFunc GetHashFunction() => static (obj, seed) =>
    {
        bool seeded = seed != 0;

        if (obj is string str)
            return DJB2Hash.ComputeHash(str.AsSpan(), seeded ? seed : DJB2Hash.Seed);

        uint hash = obj switch
        {
            char or sbyte or byte or short or ushort or int => (uint)Convert.ToInt32(obj, NumberFormatInfo.InvariantInfo),
            uint u => u,
            _ => unchecked((uint)obj.GetHashCode())
        };

        return seeded ? hash ^ seed : hash;
    };

    public EqualFunc GetEqualFunction() => static (a, b) => ((string)a).Equals((string)b, StringComparison.Ordinal);
}