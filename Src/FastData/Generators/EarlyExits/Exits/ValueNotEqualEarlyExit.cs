using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey != Value;
/// <summary>Rejects keys that differ from the required value.</summary>
/// <typeparam name="T">The key type.</typeparam>
/// <param name="Value">The only accepted value.</param>
public sealed record ValueNotEqualEarlyExit<T>(T Value) : ValueComparisonEarlyExitBase<T>(Value)
{
    /// <inheritdoc />
    public override ulong KeyspaceSize => 1;

    /// <inheritdoc />
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    /// <inheritdoc />
    public override bool IsWorseThan(IEarlyExit other) => false;
}