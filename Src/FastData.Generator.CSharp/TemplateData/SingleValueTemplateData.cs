using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.CSharp.TemplateData;

public sealed class SingleValueTemplateData : ITemplateData
{
    public required object Item { get; init; }
    public required object? Value { get; init; }
}