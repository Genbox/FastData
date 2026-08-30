namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Describes a segment in a piecewise geometric model.</summary>
public sealed class PgmSegmentTemplateData
{
    /// <summary>Gets the first key covered by the segment.</summary>
    public required object Key { get; init; }

    /// <summary>Gets the slope of the segment.</summary>
    public required float Slope { get; init; }

    /// <summary>Gets the intercept of the segment.</summary>
    public required int Intercept { get; init; }
}