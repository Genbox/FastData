using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for range lookup templates.</summary>
public sealed class RangeTemplateData : ITemplateData
{
    /// <summary>Gets the key ranges.</summary>
    public required RangeEntryTemplateData[] Ranges { get; init; }

    /// <summary>Gets the number of key ranges.</summary>
    public required int RangeCount { get; init; }
}