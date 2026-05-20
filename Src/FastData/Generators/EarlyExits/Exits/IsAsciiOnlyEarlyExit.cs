using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !IsAsciiOnly(inputKey);
public sealed record IsAsciiOnlyEarlyExit : IEarlyExit
{
    public ulong KeyspaceSize => ulong.MaxValue;

    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.IsAsciiOnly), [typeof(string)])!;
        return Not(Call(methodInfo, key));
    }

    public bool IsWorseThan(IEarlyExit other) => false;
}