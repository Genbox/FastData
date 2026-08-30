using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

/// <summary>Provides a base for early exits that compare an input key with a value.</summary>
/// <typeparam name="T">The type of value to compare.</typeparam>
/// <param name="Value">The value used by the comparison.</param>
public abstract record ValueComparisonEarlyExitBase<T>(T Value) : IEarlyExit
{
    /// <inheritdoc />
    public Expression GetExpression(ParameterExpression key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        return Compare(key, Constant(Value, key.Type));
    }

    /// <inheritdoc />
    public abstract bool IsWorseThan(IEarlyExit other);

    /// <inheritdoc />
    public abstract ulong KeyspaceSize { get; }

    /// <summary>Builds the comparison between the key and the configured value.</summary>
    /// <param name="left">The key expression.</param>
    /// <param name="right">The configured value expression.</param>
    /// <returns>The comparison expression.</returns>
    protected abstract BinaryExpression Compare(Expression left, Expression right);
}