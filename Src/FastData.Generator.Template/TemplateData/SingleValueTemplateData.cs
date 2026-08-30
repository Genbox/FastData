using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for single-value lookup templates.</summary>
public sealed class SingleValueTemplateData : ITemplateData
{
    /// <summary>Gets the single lookup key.</summary>
    public required object Item { get; init; }

    /// <summary>Gets a value indicating whether a value is associated with the key.</summary>
    public required bool HasValue { get; init; }

    /// <summary>Gets the value associated with the key, when present.</summary>
    public required object? Value { get; init; }
}