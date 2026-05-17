using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// UnitAt(inputKey, Offset) bitmap check
public sealed record UnitAtBitmapEarlyExit(ulong Low, ulong High, bool IgnoreCase, int Offset = 0) : UnitBitmapEarlyExitBase(Low, High, IgnoreCase ? nameof(GeneratorFunctions.UnitAtAsciiLower) : nameof(GeneratorFunctions.UnitAt))
{
    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(GeneratorFunctions.UnitAtAsciiLower) : nameof(GeneratorFunctions.UnitAt);
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return BuildBitmapExpression(Call(methodInfo, key, Constant(Offset)));
    }
}