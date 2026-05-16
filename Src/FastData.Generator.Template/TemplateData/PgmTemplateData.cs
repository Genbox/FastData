using Genbox.FastData.Generator.Template.Abstracts;

namespace Genbox.FastData.Generator.Template.TemplateData;

public sealed class PgmTemplateData : ITemplateData
{
    public required IEnumerable<object> Keys { get; init; }
    public required int KeyCount { get; init; }
    public required IEnumerable<object> Values { get; init; }
    public required int ValueCount { get; init; }
    public required PgmSegmentTemplateData[] Segments { get; init; }
    public required int[] LevelsOffsets { get; init; }
    public required int SegmentCount { get; init; }
    public required int Epsilon { get; init; }
    public required int EpsilonRecursive { get; init; }
}