namespace Genbox.FastData.Generator.Rust.TemplateData;

public sealed class HashTableCompactEntryTemplateData
{
    public required object? Key { get; init; }
    public required ulong Hash { get; init; }
}
