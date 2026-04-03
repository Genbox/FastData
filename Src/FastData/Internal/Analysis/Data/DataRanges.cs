namespace Genbox.FastData.Internal.Analysis.Data;

internal sealed class DataRanges<T>
{
    internal DataRanges(T min, T max)
    {
        Ranges = [(min, max)];
    }

    internal DataRanges(int numKeys)
    {
        Ranges = new List<(T Start, T End)>(numKeys / 4);
    }

    public void Add(T start, T end)
    {
        Ranges.Add((start, end));
    }

    internal List<(T Start, T End)> Ranges { get; }
    internal T Min => Ranges[0].Start;
    internal T Max => Ranges[Ranges.Count - 1].End;
}