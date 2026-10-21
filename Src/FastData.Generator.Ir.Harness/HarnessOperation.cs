namespace Genbox.FastData.Generator.Ir.Harness;

/// <summary>Operations supplied by a language harness adapter.</summary>
public enum HarnessOperation
{
    /// <summary>Tests membership.</summary>
    Contains,
    /// <summary>Tests lookup against an expected value.</summary>
    LookupMatches,
    /// <summary>Tests a missing lookup.</summary>
    LookupMiss,
    /// <summary>Counts enumerated keys.</summary>
    KeyCount,
    /// <summary>Reads an enumerated key.</summary>
    KeyAt,
    /// <summary>Counts enumerated values.</summary>
    ValueCount,
    /// <summary>Reads an enumerated value.</summary>
    ValueAt,
    /// <summary>Counts contiguous keys.</summary>
    KeySpanCount,
    /// <summary>Reads a contiguous key.</summary>
    KeySpanAt,
    /// <summary>Counts contiguous values.</summary>
    ValueSpanCount,
    /// <summary>Reads a contiguous value.</summary>
    ValueSpanAt,
    /// <summary>Reads the monotonic clock.</summary>
    Clock,
    /// <summary>Computes elapsed nanoseconds.</summary>
    Elapsed,
    /// <summary>Makes a query opaque to optimization.</summary>
    Opaque,
    /// <summary>Consumes a result.</summary>
    Consume,
    /// <summary>Reports a measurement.</summary>
    Report
}