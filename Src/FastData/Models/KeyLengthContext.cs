using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Models;

public class KeyLengthContext(List<string>?[] lengths, bool lengthsAreUniq, uint minLength, uint maxLength) : IContext
{
    public List<string>?[] Lengths { get; } = lengths;
    public bool LengthsAreUniq { get; } = lengthsAreUniq;
    public uint MinLength { get; } = minLength;
    public uint MaxLength { get; } = maxLength;
}