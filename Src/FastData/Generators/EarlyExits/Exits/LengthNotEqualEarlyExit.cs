using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// return Length(inputKey) != value;
/// <summary>Rejects strings whose length differs from the required length.</summary>
/// <param name="Value">The only accepted string length.</param>
public sealed record LengthNotEqualEarlyExit(int Value) : MethodComparisonEarlyExitBase<int>(Value, nameof(GeneratorFunctions.Length))
{
    /// <inheritdoc />
    public override ulong KeyspaceSize => 1;

    /// <inheritdoc />
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    /// <inheritdoc />
    public override bool IsWorseThan(IEarlyExit other) => false;
}