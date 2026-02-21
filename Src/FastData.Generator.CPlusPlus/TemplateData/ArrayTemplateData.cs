namespace Genbox.FastData.Generator.CPlusPlus.TemplateData;

public sealed class ArrayTemplateData : ITemplateData
{
    public required IEnumerable<object> Keys { get; init; }
    public required int KeyCount { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
