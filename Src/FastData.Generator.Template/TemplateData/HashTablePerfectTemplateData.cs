using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for perfect hash-table templates.</summary>
public sealed class HashTablePerfectTemplateData : ITemplateData
{
    /// <summary>Gets the perfect hash-table entries.</summary>
    public required HashTablePerfectEntryTemplateData[] Entries { get; init; }

    /// <summary>Gets the values associated with the entries.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of associated values.</summary>
    public required int ValueCount { get; init; }
}