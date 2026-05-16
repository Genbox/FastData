using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetCharFromEnd(inputKey, Offset) != Value;
public sealed record CharLastNotEqualEarlyExit(char Value, bool IgnoreCase, int Offset = 0) : MethodComparisonEarlyExitBase<char>(Value, IgnoreCase ? nameof(StringFunctions.GetCharFromEndLower) : nameof(StringFunctions.GetCharFromEnd))
{
    public override ulong KeyspaceSize => 1;
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(StringFunctions.GetCharFromEndLower) : nameof(StringFunctions.GetCharFromEnd);
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return Compare(Call(methodInfo, key, Constant(Offset)), Constant(Value, typeof(char)));
    }

    public override bool IsWorseThan(IEarlyExit other) => false;
}