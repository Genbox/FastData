namespace Genbox.FastData.Generator.Rust.Internal.TemplateData;

public sealed class HashTableTemplateData : ITemplateData
{
    public required HashTableEntryTemplateData[] Entries { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
