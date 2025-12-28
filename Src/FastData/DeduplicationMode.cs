namespace Genbox.FastData;

public enum DeduplicationMode : byte
{
    Disabled = 0,
    HashSet,
    HashSetThrowOnDup,
    Sort,
    SortThrowOnDup
}