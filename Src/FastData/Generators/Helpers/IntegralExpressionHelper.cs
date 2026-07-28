using System.Linq.Expressions;
using static Genbox.FastData.Generators.Helpers.TypeHelper;

namespace Genbox.FastData.Generators.Helpers;

internal static class IntegralExpressionHelper
{
    internal static (BinaryExpression Difference, ConstantExpression Range) CreateUnsignedRange(Expression key, TypeCode typeCode, ulong start, ulong range)
    {
        Type arithmeticType = typeCode is TypeCode.Int64 or TypeCode.UInt64 ? typeof(ulong) : typeof(uint);
        Expression value = key.Type == arithmeticType ? key : Convert(key, arithmeticType);
        ConstantExpression startValue = Constant(ConvertValueToType(start, arithmeticType), arithmeticType);
        ConstantExpression rangeValue = Constant(ConvertValueToType(range, arithmeticType), arithmeticType);
        return (Subtract(value, startValue), rangeValue);
    }
}