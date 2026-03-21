using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract class StringAffixEarlyExitBase(string affix, string method) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(EarlyExitFunctions).GetMethod(method, [typeof(string), typeof(string)])!;
        return Not(Call(methodInfo, Constant(affix), key));
    }
}