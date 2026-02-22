namespace Genbox.FastData;

public enum DeduplicationMode : byte
{
    /// <summary>
    /// No deduplication is performed. However, if there is a duplicate in the input, it might cause undefined behavior.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Uses a hash set to deduplicate data. It is faster than sorting, but uses more memory. It does not change the order of keys.
    /// </summary>
    HashSetPreserveOrder,

    /// <summary>
    /// Uses sorting to deduplicate data. It is not as fast as <seealso cref="HashSetPreserveOrder"/>, but it uses about half the memory. As a side effect, it changes the order of keys, which might be a desired side effect under certain circumstances.
    /// </summary>
    Sort,

    /// <summary>
    /// Same as <seealso cref="Sort"/>, but input order is preserved.
    /// </summary>
    SortPreserveOrder
}