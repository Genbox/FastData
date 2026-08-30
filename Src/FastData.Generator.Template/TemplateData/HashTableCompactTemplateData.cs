using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for compact hash-table templates.</summary>
public sealed class HashTableCompactTemplateData : ITemplateData
{
    /// <summary>Gets the compact hash-table entries.</summary>
    public required HashTableCompactEntryTemplateData[] Entries { get; init; }

    /// <summary>Gets the values associated with occupied entries.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of associated values.</summary>
    public required int ValueCount { get; init; }
}