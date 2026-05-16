using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey => ((uint)GetCharAt(inputKey, Offset) < 64u)
//     ? ((Low & (1UL << (int)(uint)GetCharAt(inputKey, Offset))) == 0UL)
//     : ((High & (1UL << (int)((uint)GetCharAt(inputKey, Offset) - 64u))) == 0UL);
public sealed record CharFirstBitmapEarlyExit(ulong Low, ulong High, bool IgnoreCase, int Offset = 0) : CharBitmapEarlyExitBase(Low, High, IgnoreCase ? nameof(StringFunctions.GetCharAtLower) : nameof(StringFunctions.GetCharAt))
{
    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(StringFunctions.GetCharAtLower) : nameof(StringFunctions.GetCharAt);
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return BuildBitmapExpression(Call(methodInfo, key, Constant(Offset)));
    }
}