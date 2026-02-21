namespace Genbox.FastData.Generator.Rust.Internal.TemplateData;

public sealed class KeyLengthTemplateData : ITemplateData
{
    public required IEnumerable<object> Keys { get; init; }
    public required int KeyCount { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
}
