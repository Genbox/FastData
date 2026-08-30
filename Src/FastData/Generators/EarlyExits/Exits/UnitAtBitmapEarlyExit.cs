using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.EarlyExits.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// UnitAt(inputKey, Offset) bitmap check
/// <summary>Rejects strings whose unit at a fixed offset is absent from the configured ASCII bitmap.</summary>
/// <param name="Low">The bitmap of accepted normalized unit values from 0 through 63.</param>
/// <param name="High">The bitmap of accepted normalized unit values from 64 through 127.</param>
/// <param name="IgnoreCase">Whether to normalize ASCII letters to lowercase.</param>
/// <param name="Offset">The start-relative index, or a negative end-relative index.</param>
public sealed record UnitAtBitmapEarlyExit(ulong Low, ulong High, bool IgnoreCase, int Offset = 0) : UnitBitmapEarlyExitBase(Low, High)
{
    /// <inheritdoc />
    public override Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(GeneratorFunctions.UnitAtAsciiLower) : nameof(GeneratorFunctions.UnitAt);
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(method, [typeof(string), typeof(int)])!;
        return BuildBitmapExpression(Call(methodInfo, key, Constant(Offset)));
    }
}