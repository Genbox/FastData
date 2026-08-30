using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// UnitAt(inputKey, Offset) != Value;
/// <summary>Rejects strings whose unit at a fixed offset differs from the required value.</summary>
/// <param name="Value">The only accepted normalized unit value.</param>
/// <param name="IgnoreCase">Whether to normalize ASCII letters to lowercase.</param>
/// <param name="Offset">The start-relative index, or a negative end-relative index.</param>
public sealed record UnitAtNotEqualEarlyExit(char Value, bool IgnoreCase, int Offset = 0) : MethodComparisonEarlyExitBase<char>(Value, IgnoreCase ? nameof(GeneratorFunctions.UnitAtAsciiLower) : nameof(GeneratorFunctions.UnitAt))
{
    /// <inheritdoc />
    public override ulong KeyspaceSize => 1;

    /// <inheritdoc />
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    /// <inheritdoc />
    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(GeneratorFunctions.UnitAtAsciiLower) : nameof(GeneratorFunctions.UnitAt);
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return Compare(Call(methodInfo, key, Constant(Offset)), Constant((uint)Value, typeof(uint)));
    }

    /// <inheritdoc />
    public override bool IsWorseThan(IEarlyExit other) => false;
}