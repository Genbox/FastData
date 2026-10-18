using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Helpers;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class BinarySearchInterpolation<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public List<IrStatement> Lower(BinarySearchInterpolationContext<TKey, TValue> context)
    {
        TKey[] keys = GetArray(context.Keys);
        IrDataId data = lowering.ArrayLookup(keys, context.Values);
        ulong range = unchecked(NumericPrediction.Coordinate(keys[keys.Length - 1]!) - NumericPrediction.Coordinate(keys[0]!));
        int shift = NumericPrediction.Shift(range);
        IrVariableId low = lowering.Builder.FindLocal("low", IrType.I32);
        IrVariableId high = lowering.Builder.FindLocal("high", IrType.I32);
        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);
        IrVariableId normalized = lowering.Builder.FindLocal("normalizedRange", IrType.U64);
        IrVariableId reciprocal = lowering.Builder.FindLocal("reciprocal", IrType.U64);
        IrVariableId offset = lowering.Builder.FindLocal("offset", IrType.U64);

        IrExpression At(IrVariableId i) => Index(Read(data), Read(i));

        IrStatement Narrow() => IfElse(Less(At(index), lowering.Key),
            Block(Assign(low, Add(Read(index), I32(1)))),
            Block(Assign(high, Subtract(Read(index), I32(1)))));

        List<IrStatement> body = [Assign(low, I32(0)), Assign(high, I32(keys.Length - 1))];

        if (range != 0)
        {
            ulong firstReciprocal = (((ulong)(keys.Length - 1) << 32) / (range >> shift)) + 1;
            body.Add(If(Not(AndAlso(GreaterOrEqual(lowering.Key, At(low)), LessOrEqual(lowering.Key, At(high)))), Return(I32(-1))));
            body.Add(Assign(offset,
                lowering.MultiplyHigh(ShiftLeft(Subtract(lowering.Builder.Keys.Coordinate(lowering.Key), U64(NumericPrediction.Coordinate(keys[0]!))), I32(32 - shift)),
                    U64(firstReciprocal))));
            body.Add(Assign(index, Read(high)));
            body.Add(If(LessOrEqual(Read(offset), Unsigned(Read(high), IrType.I32)), Assign(index, Int(Read(offset), IrType.U64))));
            body.Add(If(Equal(At(index), lowering.Key), Return(Read(index))));
            body.Add(Narrow());
        }

        body.Add(While(AndAlso(LessOrEqual(Read(low), Read(high)), AndAlso(GreaterOrEqual(lowering.Key, At(low)), LessOrEqual(lowering.Key, At(high)))),
            If(Equal(At(low), At(high)), Return(Read(low))),
            Assign(normalized, ShiftRight(Subtract(lowering.Builder.Keys.Coordinate(At(high)), lowering.Builder.Keys.Coordinate(At(low))), I32(shift))),
            IfElse(Less(Read(normalized), Unsigned(Subtract(Read(high), Read(low)), IrType.I32)),
                Block(Assign(index, Add(Read(low), HalveNonNegative(Subtract(Read(high), Read(low)))))),
                Block(Assign(reciprocal, Add(Divide(ShiftLeft(Unsigned(Subtract(Read(high), Read(low)), IrType.I32), I32(32)), Read(normalized)), U64(1))),
                    Assign(offset,
                        lowering.MultiplyHigh(ShiftLeft(Subtract(lowering.Builder.Keys.Coordinate(lowering.Key), lowering.Builder.Keys.Coordinate(At(low))), I32(32 - shift)),
                            Read(reciprocal))),
                    Assign(index, Read(high)),
                    If(LessOrEqual(Read(offset), Unsigned(Subtract(Read(high), Read(low)), IrType.I32)),
                        Assign(index, Add(Read(low), Int(Read(offset), IrType.U64)))))),
            If(Equal(At(index), lowering.Key), Return(Read(index))),
            Narrow()));
        body.Add(Return(I32(-1)));
        return body;
    }
}