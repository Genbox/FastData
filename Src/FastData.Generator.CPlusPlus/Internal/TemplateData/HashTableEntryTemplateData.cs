namespace Genbox.FastData.Generator.CPlusPlus.Internal.TemplateData;

public sealed class HashTableEntryTemplateData
{
    public required object? Key { get; init; }
    public required ulong Hash { get; init; }
    public required int Next { get; init; }
}
