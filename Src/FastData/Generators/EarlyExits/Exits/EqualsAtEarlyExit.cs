using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// !EqualsAt(inputKey, offset, fragment);
/// <summary>Rejects strings whose segment at a fixed offset does not equal the required fragment.</summary>
/// <param name="Fragment">The fragment that must match.</param>
/// <param name="Offset">The absolute or end-relative offset to compare.</param>
/// <param name="IgnoreCase">Whether to compare ASCII letters without regard to case.</param>
public sealed record EqualsAtEarlyExit(string Fragment, int Offset, bool IgnoreCase) : IEarlyExit
{
    /// <inheritdoc />
    public Expression GetExpression(ParameterExpression key)
    {
        string method = IgnoreCase ? nameof(GeneratorFunctions.EqualsAtAsciiLower) : nameof(GeneratorFunctions.EqualsAt);
        MethodInfo methodInfo = typeof(GeneratorFunctions).GetMethod(method, [typeof(string), typeof(int), typeof(string)])!;
        return Not(Call(methodInfo, key, Constant(Offset), Constant(Fragment)));
    }

    /// <inheritdoc />
    public bool IsWorseThan(IEarlyExit other) => false;

    /// <inheritdoc />
    public ulong KeyspaceSize => (ulong)Fragment.Length;
}