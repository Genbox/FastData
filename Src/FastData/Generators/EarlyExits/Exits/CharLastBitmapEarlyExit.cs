using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey => ((uint)GetCharFromEnd(inputKey, Offset) < 64u)
//    ? ((Low & (1UL << (int)(uint)GetCharFromEnd(inputKey, Offset))) == 0UL)
//    : ((High & (1UL << (int)((uint)GetCharFromEnd(inputKey, Offset) - 64u))) == 0UL);
public sealed record CharLastBitmapEarlyExit(ulong Low, ulong High, bool IgnoreCase, int Offset = 0) : CharBitmapEarlyExitBase(Low, High, IgnoreCase ? nameof(StringFunctions.GetCharFromEndLower) : nameof(StringFunctions.GetCharFromEnd))
{
    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(StringFunctions.GetCharFromEndLower) : nameof(StringFunctions.GetCharFromEnd);
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return BuildBitmapExpression(Call(methodInfo, key, Constant(Offset)));
    }
}