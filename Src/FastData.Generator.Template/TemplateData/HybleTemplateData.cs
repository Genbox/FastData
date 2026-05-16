using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

public sealed class HybleTemplateData : ITemplateData
{
    public required IEnumerable<object> Keys { get; init; }
    public required int KeyCount { get; init; }
    public required ushort[] Displacements { get; init; }
    public required uint ApproxRange { get; init; }
    public required uint BucketMask { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }

    /// <summary>
    /// The seed multiplied into the base hash during construction.
    /// The generated hash function must emit <c>hash(key) * Seed</c> before computing approx/bucket.
    /// </summary>
    public required ulong Seed { get; init; }
}