using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for bit-set templates.</summary>
public sealed class BitSetTemplateData : ITemplateData
{
    /// <summary>Gets the values stored by the bit set.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of stored values.</summary>
    public required int ValueCount { get; init; }

    /// <summary>Gets a value indicating whether the generated bit set includes occupancy data.</summary>
    public required bool HasOccupancy { get; init; }
}