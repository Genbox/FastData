using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Helpers;

internal static class ExpressionHelper
{
    internal static string Print(Mixer mixer) => mixer(Variable(typeof(ulong), "hash"), Variable(typeof(ulong), "Value")).ToString();

    internal static string Print(Avalanche avalanche) => avalanche(Variable(typeof(ulong), "hash")).ToString();

    internal static string Print(Expression exp)
    {
        PropertyInfo? propertyInfo = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);

        if (propertyInfo == null)
            throw new InvalidOperationException("Unable to get DebugView property");

        return (string)propertyInfo.GetValue(exp)!;
    }
}