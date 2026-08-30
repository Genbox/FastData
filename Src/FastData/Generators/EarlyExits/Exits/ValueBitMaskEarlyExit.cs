using System.Linq.Expressions;
using System.Numerics;
using Genbox.FastData.Generators.Abstracts;
using static Genbox.FastData.Generators.Helpers.TypeHelper;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// (inputKey & Mask) != 0;
/// <summary>Rejects integral keys that have any configured forbidden bit set.</summary>
/// <param name="Mask">The mask of bits that cannot be set in an accepted key.</param>
public sealed record ValueBitMaskEarlyExit(ulong Mask) : IEarlyExit
{
    /// <inheritdoc />
    public Expression GetExpression(ParameterExpression key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        Type keyType = key.Type;
        Type unsignedType = GetUnsignedType(keyType);
        Expression keyValue = keyType == unsignedType ? key : Convert(key, unsignedType);
        object maskValue = ConvertValueToType(Mask, unsignedType);
        Expression masked = And(keyValue, Constant(maskValue, unsignedType));
        object zeroValue = ConvertValueToType(0, unsignedType);

        return NotEqual(masked, Constant(zeroValue, unsignedType));
    }

    /// <inheritdoc />
    public bool IsWorseThan(IEarlyExit other) => false;

    /// <inheritdoc />
    public ulong KeyspaceSize => (ulong)BitOperations.PopCount(Mask);
}