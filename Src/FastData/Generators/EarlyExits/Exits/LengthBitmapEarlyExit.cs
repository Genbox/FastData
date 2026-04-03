using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// (BitSet & (1UL << (int)((GetLength(inputKey) - 1) & 63))) == 0UL;
public sealed record LengthBitmapEarlyExit(ulong BitSet) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        MethodInfo methodInfo = typeof(StringFunctions).GetMethod(nameof(StringFunctions.GetLength), [typeof(string)])!;

        Expression shift = And(Subtract(Call(methodInfo, key), Constant(1)), Constant(63));
        Expression shiftedBit = LeftShift(Constant(1UL), Convert(shift, typeof(int)));
        Expression masked = And(Constant(BitSet), shiftedBit);
        return Equal(masked, Constant(0UL));
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize
    {
        get
        {
            // Bitmap tracks observed lengths in the first 64 slots; rejected count is missing bits.
            int observed = BitOperations.PopCount(BitSet);
            return 64UL - (ulong)observed;
        }
    }
}