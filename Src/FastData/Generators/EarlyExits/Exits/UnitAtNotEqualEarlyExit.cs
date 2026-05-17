using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// UnitAt(inputKey, Offset) != Value;
public sealed record UnitAtNotEqualEarlyExit(char Value, bool IgnoreCase, int Offset = 0) : MethodComparisonEarlyExitBase<char>(Value, IgnoreCase ? nameof(GeneratorFunctions.UnitAtAsciiLower) : nameof(GeneratorFunctions.UnitAt))
{
    public override ulong KeyspaceSize => 1;
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(GeneratorFunctions.UnitAtAsciiLower) : nameof(GeneratorFunctions.UnitAt);
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return Compare(Call(methodInfo, key, Constant(Offset)), Constant((uint)Value, typeof(uint)));
    }

    public override bool IsWorseThan(IEarlyExit other) => false;
}