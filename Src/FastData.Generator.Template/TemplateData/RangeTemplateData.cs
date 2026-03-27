using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

public sealed class RangeTemplateData : ITemplateData
{
    public required object Min { get; init; }
    public required object Max { get; init; }
}