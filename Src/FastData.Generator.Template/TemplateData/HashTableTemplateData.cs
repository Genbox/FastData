using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for chained hash-table templates.</summary>
public sealed class HashTableTemplateData : ITemplateData
{
    /// <summary>Gets the hash-table entries.</summary>
    public required HashTableEntryTemplateData[] Entries { get; init; }

    /// <summary>Gets the values associated with occupied entries.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of associated values.</summary>
    public required int ValueCount { get; init; }
}