using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines the interface for early exit strategies used by code generators.</summary>
public interface IEarlyExit
{
    /// <summary>Gets a heuristic size of the keyspace this early exit rejects.</summary>
    /// <remarks>Callers divide it by the observed span of the inspected property to estimate a rejection ratio. It is not an exact count of rejected keys.</remarks>
    ulong KeyspaceSize { get; }

    /// <summary>Creates the expression that detects whether a key can exit early.</summary>
    /// <param name="key">The key expression to inspect.</param>
    /// <returns>The early-exit condition.</returns>
    Expression GetExpression(ParameterExpression key);

    /// <summary>Determines whether this early exit is less useful than another candidate.</summary>
    /// <param name="other">The candidate to compare with.</param>
    /// <returns><see langword="true" /> when <paramref name="other" /> should be preferred.</returns>
    bool IsWorseThan(IEarlyExit other);
}