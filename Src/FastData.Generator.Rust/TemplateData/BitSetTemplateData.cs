namespace Genbox.FastData.Generator.Rust.TemplateData;

public sealed class BitSetTemplateData : ITemplateData
{
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
