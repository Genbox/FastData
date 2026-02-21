namespace Genbox.FastData.Generator.Rust.Internal.TemplateData;

public sealed class RangeTemplateData : ITemplateData
{
    public required object Min { get; init; }
    public required object Max { get; init; }
}
