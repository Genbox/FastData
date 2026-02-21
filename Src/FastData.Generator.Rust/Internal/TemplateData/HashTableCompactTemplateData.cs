namespace Genbox.FastData.Generator.Rust.Internal.TemplateData;

public sealed class HashTableCompactTemplateData : ITemplateData
{
    public required HashTableCompactEntryTemplateData[] Entries { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
