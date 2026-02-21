namespace Genbox.FastData.Generator.Rust.TemplateData;

public sealed class HashTablePerfectTemplateData : ITemplateData
{
    public required HashTablePerfectEntryTemplateData[] Entries { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
