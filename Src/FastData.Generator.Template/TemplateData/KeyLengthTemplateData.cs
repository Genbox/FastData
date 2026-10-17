using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for key-length lookup templates.</summary>
public sealed class KeyLengthTemplateData : ITemplateData
{
    /// <summary>Gets the keys indexed by length minus the shortest key length, with <see langword="null" /> where no key has that length.</summary>
    public required IEnumerable<object> Keys { get; init; }

    /// <summary>Gets the number of length slots.</summary>
    public required int KeyCount { get; init; }

    /// <summary>Gets the values associated with the keys.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of associated values.</summary>
    public required int ValueCount { get; init; }
}