namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Describes an entry in a compact hash-table template.</summary>
public sealed class HashTableCompactEntryTemplateData
{
    /// <summary>Gets the entry key, or <see langword="null"/> for an unused slot.</summary>
    public required object? Key { get; init; }

    /// <summary>Gets the precomputed hash of the entry key.</summary>
    public required ulong Hash { get; init; }
}