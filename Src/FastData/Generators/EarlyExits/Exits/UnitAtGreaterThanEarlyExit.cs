using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// UnitAt(inputKey, Offset) > Value;
public sealed record UnitAtGreaterThanEarlyExit(char Value, int Offset = 0) : MethodComparisonEarlyExitBase<char>(Value, nameof(GeneratorFunctions.UnitAt))
{
    public override ulong KeyspaceSize => (ulong)(char.MaxValue - Value);
    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);

    public override Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.UnitAt), [typeof(string), typeof(int)])!;
        return Compare(Call(methodInfo, key, Constant(Offset)), Constant((uint)Value, typeof(uint)));
    }

    public override bool IsWorseThan(IEarlyExit other) => other is UnitAtGreaterThanEarlyExit otherExit && Offset == otherExit.Offset && Value > otherExit.Value;
}