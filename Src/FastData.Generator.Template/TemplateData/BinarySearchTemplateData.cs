using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for binary-search templates.</summary>
public sealed class BinarySearchTemplateData : ITemplateData
{
    /// <summary>Gets the ordered lookup keys.</summary>
    public required IEnumerable<object> Keys { get; init; }

    /// <summary>Gets the number of lookup keys.</summary>
    public required int KeyCount { get; init; }

    /// <summary>Gets the values associated with the lookup keys.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of associated values.</summary>
    public required int ValueCount { get; init; }
}