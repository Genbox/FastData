using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class Range<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, (TKey Start, TKey End)[] ranges)
    {
        IrDataId starts = lowering.Builder.Program.AddData("rangeStarts", IrClrConverter.FromValue(ranges.Select(static x => x.Start).ToArray()));
        IrDataId ends = lowering.Builder.Program.AddData("rangeEnds", IrClrConverter.FromValue(ranges.Select(static x => x.End).ToArray()));

        // Enumeration positions count keys across ranges; firsts[r] is the position of the first key of range r.
        long[] firsts = new long[ranges.Length];
        long total = 0;

        for (int r = 0; r < ranges.Length; r++)
        {
            firsts[r] = total;
            total = checked(total + (long)unchecked(ToUnsigned(ranges[r].End) - ToUnsigned(ranges[r].Start)) + 1);
        }

        IrDataId firstData = lowering.Builder.Program.AddData("rangeFirsts", IrClrConverter.FromValue(firsts));
        AddRangeCursor(lowering, total, starts, firstData, ranges.Length);

        IrExpression StartAt(IrExpression index) => Index(Read(starts), index);
        IrExpression EndAt(IrExpression index) => Index(Read(ends), index);

        if (ranges.Length == 1)
            return [If(AndAlso(GreaterOrEqual(lowering.Key, StartAt(I32(0))), LessOrEqual(lowering.Key, EndAt(I32(0)))), Return(I32(0))), Return(I32(-1))];

        IrVariableId low = lowering.Builder.FindLocal("low", IrType.I32);

        if (ranges.Length < 8)
        {
            return
            [
                Assign(low, I32(0)),
                While(Less(Read(low), I32(ranges.Length)),
                    If(Less(lowering.Key, StartAt(Read(low))), Return(I32(-1))),
                    If(LessOrEqual(lowering.Key, EndAt(Read(low))), Return(I32(0))),
                    Increment(low)),
                Return(I32(-1))
            ];
        }

        // The candidate is the last range that starts at or before the key.
        List<IrStatement> statements = LowerBound(lowering.Builder.FindLocal, low, ranges.Length, index => LessOrEqual(StartAt(index), lowering.Key));
        statements.Add(Assign(low, Subtract(Read(low), I32(1))));
        statements.Add(If(AndAlso(GreaterOrEqual(Read(low), I32(0)), LessOrEqual(lowering.Key, EndAt(Read(low)))), Return(I32(0))));
        statements.Add(Return(I32(-1)));
        return statements;
    }

    private static void AddRangeCursor(LoweringContext<TKey, TValue> lowering, long total, IrDataId starts, IrDataId firsts, int rangeCount)
    {
        IrFunctionId next = lowering.Enumeration.AddNext(total - 1);
        IrFunctionBuilder keyAt = lowering.Builder.Program.Function("KeyAt", lowering.KeyType);
        IrVariableId position = keyAt.Parameter("position", IrType.I64);
        IrVariableId range = keyAt.Local("range", IrType.I32);
        IrExpression offset = Convert(IrType.U64, Subtract(Read(position), Index(Read(firsts), Read(range))));
        IrExpression start = Convert(IrType.U64, Index(Read(starts), Read(range)));

        // The containing range is the last one whose first position is at or before the requested position.
        List<IrStatement> statements = LowerBound(keyAt.Local, range, rangeCount, index => LessOrEqual(Index(Read(firsts), index), Read(position)));
        statements.Add(Assign(range, Subtract(Read(range), I32(1))));
        statements.Add(Return(Convert(lowering.KeyType, Add(start, offset))));
        keyAt.Define(new IrBlock(statements));
        lowering.Builder.Enumeration = new LookupEnumeration(next, keyAt.Id);
    }
}