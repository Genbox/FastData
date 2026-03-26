using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract record StringAffixEarlyExitBase(string Affix, string Method) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(EarlyExitFunctions).GetMethod(Method, [typeof(string), typeof(string)])!;
        return Not(Call(methodInfo, Constant(Affix), key));
    }
}