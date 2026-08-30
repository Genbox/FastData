namespace Genbox.FastData.Config;

/// <summary>Defines keys for advanced per-structure generation settings.</summary>
public static class KnownSettings
{
    /// <summary>The key for the hash-table capacity multiplier.</summary>
    public const string HashTableCapacityFactor = nameof(HashTableCapacityFactor);

    /// <summary>The key controlling hash-table bucket-size optimization.</summary>
    public const string OptimizeHashTableBucketSize = nameof(OptimizeHashTableBucketSize);

    /// <summary>The key controlling whether modulo divisors are rounded to a power of two.</summary>
    public const string RoundModuloToPowerOfTwo = nameof(RoundModuloToPowerOfTwo);

    /// <summary>The key for the minimum relative rounding overhead allowed when rounding a modulo divisor to a power of two.</summary>
    public const string RoundModuloToPowerOfTwoThreshold = nameof(RoundModuloToPowerOfTwoThreshold);

    /// <summary>The key for the Elias-Fano skip-index sampling interval.</summary>
    public const string EliasFanoSkipQuantum = nameof(EliasFanoSkipQuantum);

    /// <summary>The key for the maximum value-range multiplier used by dense integral structures.</summary>
    public const string DenseIntegralValueMaxRangeFactor = nameof(DenseIntegralValueMaxRangeFactor);

    /// <summary>The key for the error tolerance of PGM leaf segments.</summary>
    public const string PgmEpsilon = nameof(PgmEpsilon);

    /// <summary>The key for the error tolerance of recursive PGM levels.</summary>
    public const string PgmEpsilonRecursive = nameof(PgmEpsilonRecursive);
}