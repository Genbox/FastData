namespace Genbox.FastData.Generator.CPlusPlus.TemplateData;

public sealed class HashTableTemplateData : ITemplateData
{
    public required HashTableEntryTemplateData[] Entries { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
