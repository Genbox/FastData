namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Describes an inclusive key range.</summary>
public sealed class RangeEntryTemplateData
{
    /// <summary>Gets the first key in the range.</summary>
    public required object Start { get; init; }

    /// <summary>Gets the last key in the range.</summary>
    public required object End { get; init; }
}