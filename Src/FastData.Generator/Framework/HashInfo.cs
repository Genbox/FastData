namespace Genbox.FastData.Generator.Framework;

public sealed class HashInfo(bool hasZeroOrNaN, StringHashInfo? stringHash)
{
    public bool HasZeroOrNaN { get; } = hasZeroOrNaN;
    public StringHashInfo? StringHash { get; } = stringHash;
}