namespace Genbox.FastData.Generator.CPlusPlus.TemplateData;

public sealed class SingleValueTemplateData : ITemplateData
{
    public required object Item { get; init; }
    public required object? Value { get; init; }
}
