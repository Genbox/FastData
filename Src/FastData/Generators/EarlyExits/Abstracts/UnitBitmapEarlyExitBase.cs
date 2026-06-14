using System.Linq.Expressions;
using System.Numerics;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Abstracts;

public abstract record UnitBitmapEarlyExitBase(ulong Low, ulong High) : IEarlyExit
{
    public abstract Expression GetExpression(ParameterExpression key);

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize
    {
        get
        {
            // Bitmap represents observed ASCII units; rejected count is missing values in the 0..127 domain.
            int observed = BitOperations.PopCount(Low) + BitOperations.PopCount(High);
            return 128UL - (ulong)observed;
        }
    }

    protected Expression BuildBitmapExpression(Expression charCall)
    {
        Expression valueExpr = Convert(charCall, typeof(uint));
        Expression bitIndex = And(valueExpr, Constant(63u));
        Expression bitShift = LeftShift(Constant(1UL), Convert(bitIndex, typeof(int)));

        if (Low == 0 && High == 0)
            throw new InvalidOperationException("Both bitmaps were zero. This should not happen.");

        // When one bitmap is zero, the branchless two-bitmap select is unnecessary.
        // We emit a simpler single-bitmap check with a range guard.
        if (Low == 0)
        {
            // All observed chars are in the high half (64-127). Chars below 64 are always rejected.
            Expression highCheck = Equal(And(Constant(High), bitShift), Constant(0UL));
            Expression isLow = LessThan(valueExpr, Constant(64u));
            return OrElse(isLow, highCheck);
        }

        if (High == 0)
        {
            // All observed chars are in the low half (0-63). Chars at or above 64 are always rejected.
            Expression lowCheck = Equal(And(Constant(Low), bitShift), Constant(0UL));
            Expression isHigh = GreaterThanOrEqual(valueExpr, Constant(64u));
            return OrElse(isHigh, lowCheck);
        }

        // General case: branchless two-bitmap select.
        Expression lowMasked = And(Constant(Low), bitShift);
        Expression highMasked = And(Constant(High), bitShift);

        Expression isHighBit = RightShift(valueExpr, Constant(6));
        Expression highMask = Subtract(Constant(0UL), Convert(isHighBit, typeof(ulong)));
        Expression lowMask = Not(highMask);

        Expression selected = Or(And(lowMasked, lowMask), And(highMasked, highMask));
        return Equal(selected, Constant(0UL));
    }
}