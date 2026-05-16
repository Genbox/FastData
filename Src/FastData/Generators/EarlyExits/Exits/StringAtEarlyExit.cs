using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !StringAt(fragment, offset, inputKey);
public sealed record StringAtEarlyExit(string Fragment, int Offset, bool IgnoreCase) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(StringFunctions.StringAtIgnoreCase) : nameof(StringFunctions.StringAt);
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(method, [typeof(string), typeof(int), typeof(string)])!;
        return Not(Call(methodInfo, Constant(Fragment), Constant(Offset), key));
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize => (ulong)Fragment.Length;
}