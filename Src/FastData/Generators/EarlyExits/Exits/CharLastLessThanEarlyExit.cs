using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetCharFromEnd(inputKey, Offset) < Value;
public sealed record CharLastLessThanEarlyExit(char Value, int Offset = 0) : MethodComparisonEarlyExitBase<char>(Value, nameof(StringFunctions.GetCharFromEnd))
{
    public override ulong KeyspaceSize => Value;
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);

    public override Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(nameof(StringFunctions.GetCharFromEnd), [typeof(string), typeof(int)])!;
        return Compare(Call(methodInfo, key, Constant(Offset)), Constant(Value, typeof(char)));
    }

    public override bool IsWorseThan(IEarlyExit other) => other is CharLastLessThanEarlyExit otherExit && Value < otherExit.Value;
}