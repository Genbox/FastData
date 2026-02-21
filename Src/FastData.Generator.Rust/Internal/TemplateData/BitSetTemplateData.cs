namespace Genbox.FastData.Generator.Rust.Internal.TemplateData;

public sealed class BitSetTemplateData : ITemplateData
{
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
