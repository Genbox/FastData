using System.Runtime.InteropServices;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Lowering;

internal static class LoweringHelpers
{
    public static IrBinaryExpression Xor(IrExpression a, IrExpression b) => Binary(BinaryOperation.ExclusiveOr, a, b);
    public static IrBinaryExpression Mod(IrExpression value, IrExpression divisor) => Binary(BinaryOperation.Modulo, value, divisor);
    public static IrBinaryExpression Divide(IrExpression value, IrExpression divisor) => Binary(BinaryOperation.Divide, value, divisor);
    public static IrExpression Int(IrExpression value, IrType source) => Convert(IrType.I32, value, source);
    public static IrExpression Unsigned(IrExpression value, IrType source) => Convert(IrType.U64, value, source);

    /// <summary>Sets <paramref name="low" /> to the first index in [0, count) for which <paramref name="before" /> is false, or to count.</summary>
    /// <remarks><paramref name="before" /> must be true for a prefix of the indices and false for the rest.</remarks>
    public static List<IrStatement> LowerBound(Func<string, IrType, IrVariableId> local, IrVariableId low, int count, Func<IrExpression, IrExpression> before)
    {
        IrVariableId high = local("high", IrType.I32);
        IrVariableId middle = local("middle", IrType.I32);
        return
        [
            Assign(low, I32(0)),
            Assign(high, I32(count)),
            While(Less(Read(low), Read(high)),
                Assign(middle, Add(Read(low), HalveNonNegative(Subtract(Read(high), Read(low))))),
                IfElse(before(Read(middle)), Block(Assign(low, Add(Read(middle), I32(1)))), Block(Assign(high, Read(middle)))))
        ];
    }

    // Reuse complete backing arrays; materialize slices and non-array memory.
    public static T[] GetArray<T>(ReadOnlyMemory<T> memory) =>
        MemoryMarshal.TryGetArray(memory, out ArraySegment<T> segment) && segment.Offset == 0 && segment.Count == segment.Array!.Length
            ? segment.Array
            : memory.ToArray();

    /// <summary>(I32)((U32)value >> 1): shifts take unsigned operands, so halving a nonnegative I32 goes through U32.</summary>
    public static IrExpression HalveNonNegative(IrExpression value) => Convert(IrType.I32, Binary(BinaryOperation.ShiftRight, Convert(IrType.U32, value, IrType.I32), I32(1)), IrType.U32);
}