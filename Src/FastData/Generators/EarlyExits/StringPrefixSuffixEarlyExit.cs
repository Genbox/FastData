using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>If all strings have a common prefix or suffix, this early exit will check for it.</summary>
public sealed record StringPrefixSuffixEarlyExit(string prefix, string suffix) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        if (prefix.Length == 0 && suffix.Length == 0)
            return Expression.Constant(false);

        MethodInfo? startsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)]);
        MethodInfo? endsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string), typeof(StringComparison)]);

        if (startsWith == null || endsWith == null)
            throw new InvalidOperationException("Could not find string comparison method.");

        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        Expression comparison = Expression.Constant(StringComparison.Ordinal);

        Expression? prefixCheck = prefix.Length == 0
            ? null
            : Expression.Call(key, startsWith, Expression.Constant(prefix), comparison);
        Expression? suffixCheck = suffix.Length == 0
            ? null
            : Expression.Call(key, endsWith, Expression.Constant(suffix), comparison);

        Expression condition;
        if (prefixCheck == null)
            condition = suffixCheck!;
        else if (suffixCheck == null)
            condition = prefixCheck;
        else
            condition = Expression.AndAlso(prefixCheck, suffixCheck);

        return Expression.Not(condition);
    }
}