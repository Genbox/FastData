using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !EqualsAt(inputKey, offset, fragment);
public sealed record EqualsAtEarlyExit(string Fragment, int Offset, bool IgnoreCase) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(GeneratorFunctions.EqualsAtAsciiLower) : nameof(GeneratorFunctions.EqualsAt);
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(method, [typeof(string), typeof(int), typeof(string)])!;
        return Not(Call(methodInfo, key, Constant(Offset), Constant(Fragment)));
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize => (ulong)Fragment.Length;
}