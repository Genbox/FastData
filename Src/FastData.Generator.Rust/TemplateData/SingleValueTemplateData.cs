namespace Genbox.FastData.Generator.Rust.TemplateData;

public sealed class SingleValueTemplateData : ITemplateData
{
    public required object Item { get; init; }
    public required object? Value { get; init; }
}
