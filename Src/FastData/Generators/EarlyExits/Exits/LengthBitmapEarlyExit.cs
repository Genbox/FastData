using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// (BitSet & (1UL << (int)((GetLength(inputKey) - 1) & 63))) == 0UL;
public sealed record LengthBitmapEarlyExit(ulong BitSet) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(EarlyExitFunctions).GetMethod(nameof(EarlyExitFunctions.GetLength), [typeof(string)])!;

        Expression shift = And(Subtract(Call(methodInfo, key), Constant(1u)), Constant(63u));
        Expression shiftedBit = LeftShift(Constant(1UL), Convert(shift, typeof(int)));
        Expression masked = And(Constant(BitSet), shiftedBit);
        return Equal(masked, Constant(0UL));
    }
}