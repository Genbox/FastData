using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// GetCharAt(inputKey, Offset) != Value;
public sealed record CharOffsetNotEqualEarlyExit(char Value, bool IgnoreCase, int Offset = 0) : MethodComparisonEarlyExitBase<char>(Value, IgnoreCase ? nameof(StringFunctions.GetCharAtLower) : nameof(StringFunctions.GetCharAt))
{
    public override ulong KeyspaceSize => 1;
    protected override BinaryExpression Compare(Expression left, Expression right) => NotEqual(left, right);

    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(StringFunctions.GetCharAtLower) : nameof(StringFunctions.GetCharAt);
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return Compare(Call(methodInfo, key, Constant(Offset)), Constant(Value, typeof(char)));
    }

    public override bool IsWorseThan(IEarlyExit other) => false;
}