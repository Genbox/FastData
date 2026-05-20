namespace Genbox.FastData.Internal.Pgm;

/// <summary>Represents a single segment in a PGM index level. Maps keys to approximate positions via a linear model: pos = slope * (key - Key) + intercept.</summary>
/// <remarks>
/// Matches the reference Segment (pgm_index.hpp). The reference uses uint32_t for intercept; we use
/// int which is equivalent since the value is always non-negative by construction. Slope is float,
/// matching the reference default Floating template parameter.
/// </remarks>
public readonly struct PgmSegment<T> where T : notnull
{
    /// <summary>Gets the first key that this segment indexes.</summary>
    public T Key { get; }

    /// <summary>Gets the slope of the linear model.</summary>
    public float Slope { get; }

    /// <summary>Gets the intercept of the linear model. Always non-negative by construction.</summary>
    public int Intercept { get; }

    public PgmSegment(T key, float slope, int intercept)
    {
        Key = key;
        Slope = slope;
        Intercept = intercept;
    }

    internal PgmSegment(OptimalPiecewiseLinearModel<T>.CanonicalSegment cs)
    {
        Key = cs.GetFirstKey();
        (double slope, double intercept) = cs.GetFloatingPointSegment(Key);

        if (intercept < 0)
            throw new OverflowException("Unexpected PGM segment intercept < 0.");

        if (intercept > int.MaxValue)
            throw new OverflowException("Unexpected PGM segment intercept > int.MaxValue.");

        Slope = (float)slope;
        Intercept = (int)intercept;
    }

    public int Evaluate(T key)
    {
        unchecked
        {
            double diff;
            if (typeof(T) == typeof(int))
            {
                uint k = (uint)(int)(object)key;
                uint s = (uint)(int)(object)Key;
                diff = k - s;
            }
            else if (typeof(T) == typeof(long))
            {
                ulong k = (ulong)(long)(object)key;
                ulong s = (ulong)(long)(object)Key;
                diff = k - s;
            }
            else if (typeof(T) == typeof(uint))
            {
                uint k = (uint)(object)key;
                uint s = (uint)(object)Key;
                diff = k - s;
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong k = (ulong)(object)key;
                ulong s = (ulong)(object)Key;
                diff = k - s;
            }
            else if (PgmTypeTraits<T>.IsFloatingPoint)
                diff = PgmTypeTraits<T>.ToDouble(key) - PgmTypeTraits<T>.ToDouble(Key);
            else
            {
                long k = PgmTypeTraits<T>.ToInt64(key);
                long s = PgmTypeTraits<T>.ToInt64(Key);
                diff = k - s;
            }

            // Matches reference: truncate slope*diff to integer first, then add intercept
            // as integer arithmetic. The reference does: size_t(slope * diff) + intercept.
            int pos = (int)(long)(Slope * diff);
            return pos + Intercept;
        }
    }
}