using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract record CharBitmapEarlyExitBase(ulong Low, ulong High, string Method) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(Method, [typeof(string)])!;

        Expression valueExpr = Convert(Call(methodInfo, key), typeof(uint));
        Expression bitIndex = And(valueExpr, Constant(63u));
        Expression bitShift = LeftShift(Constant(1UL), Convert(bitIndex, typeof(int)));

        Expression lowMasked = And(Constant(Low), bitShift);
        Expression highMasked = And(Constant(High), bitShift);

        Expression isHigh = RightShift(valueExpr, Constant(6));
        Expression highMask = Subtract(Constant(0UL), Convert(isHigh, typeof(ulong)));
        Expression lowMask = Not(highMask);

        Expression selected = Or(And(lowMasked, lowMask), And(highMasked, highMask));
        return Equal(selected, Constant(0UL));
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize
    {
        get
        {
            // Bitmap represents observed ASCII chars; rejected count is missing values in the 0..127 domain.
            int observed = BitOperations.PopCount(Low) + BitOperations.PopCount(High);
            return 128UL - (ulong)observed;
        }
    }
}
