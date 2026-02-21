using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.CSharp.TemplateData;

public sealed class BinarySearchTemplateData : ITemplateData
{
    public required IEnumerable<object> Keys { get; init; }
    public required int KeyCount { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}