using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

public sealed class RangeTemplateData : ITemplateData
{
    public required RangeEntryTemplateData[] Ranges { get; init; }
    public required int RangeCount { get; init; }
}