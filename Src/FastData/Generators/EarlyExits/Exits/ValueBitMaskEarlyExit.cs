using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using static Genbox.FastData.Generators.Helpers.TypeHelper;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// (inputKey & Mask) != 0;
public sealed record ValueBitMaskEarlyExit(ulong Mask) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        Type keyType = key.Type;
        Type unsignedType = GetUnsignedType(keyType);
        Expression keyValue = keyType == unsignedType ? key : Convert(key, unsignedType);
        object maskValue = ConvertValueToType(Mask, unsignedType);
        Expression masked = And(keyValue, Constant(maskValue, unsignedType));
        object zeroValue = ConvertValueToType(0, unsignedType);

        return NotEqual(masked, Constant(zeroValue, unsignedType));
    }
}