namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Describes an entry in a perfect hash-table template.</summary>
public sealed class HashTablePerfectEntryTemplateData
{
    /// <summary>Gets the entry key.</summary>
    public required object Key { get; init; }

    /// <summary>Gets the precomputed hash of the entry key.</summary>
    public required ulong Hash { get; init; }
}