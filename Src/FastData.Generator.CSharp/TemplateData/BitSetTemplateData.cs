using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.CSharp.TemplateData;

public sealed class BitSetTemplateData : ITemplateData
{
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}