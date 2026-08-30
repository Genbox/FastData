using System.Linq.Expressions;

namespace Genbox.FastData.Generators.Abstracts;

/// <summary>Defines the interface for early exit strategies used by code generators.</summary>
public interface IEarlyExit
{
    /// <summary>Gets the number of keys rejected by this early exit.</summary>
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