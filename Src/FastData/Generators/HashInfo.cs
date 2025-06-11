namespace Genbox.FastData.Generators;

public sealed class HashInfo(bool hasZeroOrNaN)
{
    public bool HasZeroOrNaN { get; } = hasZeroOrNaN;
}