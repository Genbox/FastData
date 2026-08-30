using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

/// <summary>Provides a base for early exits that compare a generator helper result with a value.</summary>
/// <typeparam name="T">The type of value to compare.</typeparam>
/// <param name="Value">The value used by the comparison.</param>
/// <param name="Method">The name of the <see cref="GeneratorFunctions" /> method to invoke.</param>
public abstract record MethodComparisonEarlyExitBase<T>(T Value, string Method) : IEarlyExit
{
    /// <inheritdoc />
    public virtual Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(Method, [typeof(string)])!;
        return Compare(Call(methodInfo, key), Constant(Value, typeof(T)));
    }

    /// <inheritdoc />
    public abstract bool IsWorseThan(IEarlyExit other);

    /// <inheritdoc />
    public abstract ulong KeyspaceSize { get; }

    /// <summary>Builds the comparison between the helper result and the configured value.</summary>
    /// <param name="left">The helper result expression.</param>
    /// <param name="right">The configured value expression.</param>
    /// <returns>The comparison expression.</returns>
    protected abstract BinaryExpression Compare(Expression left, Expression right);
}