using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Specs.Hash;

public sealed class DefaultHashSpec : IHashSpec
{
    private DefaultHashSpec() { }
    public static DefaultHashSpec Instance { get; } = new DefaultHashSpec();

    public HashFunc GetHashFunction() => static obj =>
    {
        if (obj is string str)
            return DJB2Hash.ComputeHash(str.AsSpan());

        uint hash = obj switch
        {
            char val => val,
            sbyte val => (uint)val,
            byte val => val,
            short val => (uint)val,
            ushort val => val,
            int val => (uint)val,
            uint val => val,
            _ => (uint)obj.GetHashCode()
        };

        return hash;
    };

    public EqualFunc GetEqualFunction() => static (a, b) => ((string)a).Equals((string)b, StringComparison.Ordinal);
}