using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for key-length lookup templates.</summary>
public sealed class KeyLengthTemplateData : ITemplateData
{
    /// <summary>Gets the key lengths represented by the template.</summary>
    public required IEnumerable<object> Keys { get; init; }

    /// <summary>Gets the number of represented key lengths.</summary>
    public required int KeyCount { get; init; }

    /// <summary>Gets the values associated with the key lengths.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of associated values.</summary>
    public required int ValueCount { get; init; }
}