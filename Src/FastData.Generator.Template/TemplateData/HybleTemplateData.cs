using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

/// <summary>Provides data for hybrid bucket lookup templates.</summary>
public sealed class HybleTemplateData : ITemplateData
{
    /// <summary>Gets the lookup keys.</summary>
    public required IEnumerable<object> Keys { get; init; }

    /// <summary>Gets the number of lookup keys.</summary>
    public required int KeyCount { get; init; }

    /// <summary>Gets the displacement assigned to each bucket.</summary>
    public required ushort[] Displacements { get; init; }

    /// <summary>Gets the approximate range used to map hashes to buckets.</summary>
    public required uint ApproxRange { get; init; }

    /// <summary>Gets the mask used to select a bucket.</summary>
    public required uint BucketMask { get; init; }

    /// <summary>Gets the values associated with the lookup keys.</summary>
    public required IEnumerable<object> Values { get; init; }

    /// <summary>Gets the number of associated values.</summary>
    public required int ValueCount { get; init; }

    /// <summary>
    /// The seed multiplied into the base hash during construction.
    /// The generated hash function must emit <c>hash(key) * Seed</c> before computing approx/bucket.
    /// </summary>
    public required ulong Seed { get; init; }
}