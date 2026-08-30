using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !IsAsciiOnly(inputKey);
/// <summary>Rejects strings that contain one or more non-ASCII characters.</summary>
public sealed record IsAsciiOnlyEarlyExit : IEarlyExit
{
    /// <inheritdoc />
    public ulong KeyspaceSize => ulong.MaxValue;

    /// <inheritdoc />
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(nameof(GeneratorFunctions.IsAsciiOnly), [typeof(string)])!;
        return Not(Call(methodInfo, key));
    }

    /// <inheritdoc />
    public bool IsWorseThan(IEarlyExit other) => false;
}