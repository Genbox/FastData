using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class EliasFano<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, EliasFanoContext context)
    {
        IrFunctionCallExpression PopCount(IrExpression value) => lowering.Builder.Builtins.Call(IrBuiltinOperation.PopCount, value);
        IrFunctionCallExpression TrailingZeros(IrExpression value) => lowering.Builder.Builtins.Call(IrBuiltinOperation.TrailingZeroCount, value);

        IrDataId upper = lowering.Builder.Program.AddData("upperBits", IrClrConverter.FromValue(context.UpperBits));
        IrDataId samples = lowering.Builder.Program.AddData("samplePositions", IrClrConverter.FromValue(context.SamplePositions));
        IrFunctionBuilder select = lowering.Builder.Program.Function("SelectZero", IrType.I64);
        IrVariableId wanted = select.Parameter("rank", IrType.I64);
        IrVariableId sample = select.Local("sample", IrType.I32);
        IrVariableId zeroRank = select.Local("zeroRank", IrType.I64);
        IrVariableId wordIndex = select.Local("word", IrType.I32);
        IrVariableId startBit = select.Local("startBit", IrType.I32);
        IrVariableId zeros = select.Local("zeros", IrType.U64);
        IrVariableId count = select.Local("count", IrType.I32);
        IrExpression samplePosition = Index(Read(samples), Read(sample));
        List<IrStatement> selectBody =
        [
            Assign(sample, Int(ShiftRight(Unsigned(Read(wanted)), I32(context.SampleRateShift)))),
            If(GreaterOrEqual(Read(sample), I32(context.SamplePositions.Length)), Return(I64(-1))),
            Assign(zeroRank, Convert(IrType.I64, ShiftLeft(Unsigned(Read(sample)), I32(context.SampleRateShift)))),
            Assign(wordIndex, Int(ShiftRight(Unsigned(samplePosition), I32(6)))),
            Assign(startBit, Int(And(Unsigned(samplePosition), U64(63))))
        ];
        List<IrStatement> scan = [Assign(zeros, Xor(Index(Read(upper), Read(wordIndex)), U64(ulong.MaxValue)))];
        int lastBits = context.UpperBitLength & 63;
        if (lastBits != 0)
            scan.Add(If(Equal(Read(wordIndex), I32(context.UpperBits.Length - 1)), Assign(zeros, And(Read(zeros), U64((1UL << lastBits) - 1)))));
        scan.Add(Assign(zeros, And(Read(zeros), ShiftLeft(U64(ulong.MaxValue), Read(startBit)))));
        scan.Add(Assign(startBit, I32(0)));
        scan.Add(Assign(count, PopCount(Read(zeros))));
        scan.Add(If(Greater(Add(Read(zeroRank), Convert(IrType.I64, Read(count))), Read(wanted)),
            While(Less(Read(zeroRank), Read(wanted)),
                Assign(zeros, And(Read(zeros), Subtract(Read(zeros), U64(1)))),
                Assign(zeroRank, Add(Read(zeroRank), I64(1)))),
            Return(Add(Multiply(Convert(IrType.I64, Read(wordIndex)), I64(64)),
                Convert(IrType.I64, TrailingZeros(Read(zeros)))))));
        scan.Add(Assign(zeroRank, Add(Read(zeroRank), Convert(IrType.I64, Read(count)))));
        scan.Add(Increment(wordIndex));
        selectBody.Add(While(Less(Read(wordIndex), I32(context.UpperBits.Length)), scan.ToArray()));
        selectBody.Add(Return(I64(-1)));
        select.Define(new IrBlock(selectBody));

        IrVariableId value = lowering.Builder.FindLocal("value", IrType.U64);
        IrVariableId high = lowering.Builder.FindLocal("high", IrType.I64);
        IrVariableId position = lowering.Builder.FindLocal("position", IrType.I64);
        IrVariableId rank = lowering.Builder.FindLocal("rank", IrType.I64);
        IrVariableId word = lowering.Builder.FindLocal("word", IrType.I32);
        IrVariableId window = lowering.Builder.FindLocal("window", IrType.U64);
        IrVariableId currentHigh = lowering.Builder.FindLocal("currentHigh", IrType.I64);
        NumericGeneratorConfig numeric = (NumericGeneratorConfig)lowering.Config;
        List<IrStatement> body =
        [
            If(OrElse(Less(lowering.Key, IrClrConverter.FromValue(numeric.Constants.MinValue)),
                    Greater(lowering.Key, IrClrConverter.FromValue(numeric.Constants.MaxValue))),
                Return(I32(-1))),
            Assign(value, Subtract(Unsigned(lowering.Key), U64(unchecked((ulong)context.MinValue)))),
            Assign(high, Convert(IrType.I64, ShiftRight(Read(value), I32(context.LowerBitCount)))),
            Assign(position, I64(0)),
            If(Greater(Read(high), I64(0)),
                Assign(position, Call(select.Id, Subtract(Read(high), I64(1)))),
                If(Less(Read(position), I64(0)), Return(I32(-1))),
                Assign(position, Add(Read(position), I64(1)))),
            Assign(rank, Subtract(Read(position), Read(high))),
            If(GreaterOrEqual(Unsigned(Read(rank)), U64(lowering.Config.ItemCount)), Return(I32(-1))),
            Assign(word, Int(ShiftRight(Unsigned(Read(position)), I32(6)))),
            If(GreaterOrEqual(Read(word), I32(context.UpperBits.Length)), Return(I32(-1))),
            Assign(window,
                And(Index(Read(upper), Read(word)), ShiftLeft(U64(ulong.MaxValue), Int(And(Unsigned(Read(position)), U64(63))))))
        ];
        List<IrStatement> loop =
        [
            While(Equal(Read(window), U64(0)),
                Increment(word),
                If(GreaterOrEqual(Read(word), I32(context.UpperBits.Length)), Return(I32(-1))),
                Assign(window, Index(Read(upper), Read(word)))),
            Assign(currentHigh,
                Subtract(Add(Multiply(Convert(IrType.I64, Read(word)), I64(64)), Convert(IrType.I64, TrailingZeros(Read(window)))),
                    Read(rank))),
            If(Greater(Read(currentHigh), Read(high)), Return(I32(-1)))
        ];

        if (context.LowerBitCount == 0)
            loop.Add(If(Equal(Read(currentHigh), Read(high)), Return(I32(0))));
        else
        {
            IrDataId lower = lowering.Builder.Program.AddData("lowerBits", IrClrConverter.FromValue(context.LowerBits));
            IrVariableId lowOffset = lowering.Builder.FindLocal("lowOffset", IrType.U64);
            IrVariableId lowWord = lowering.Builder.FindLocal("lowWord", IrType.I32);
            IrVariableId bit = lowering.Builder.FindLocal("lowBit", IrType.I32);
            IrVariableId currentLow = lowering.Builder.FindLocal("currentLow", IrType.U64);
            IrExpression target = And(Read(value), U64(context.LowerMask));
            loop.Add(If(Equal(Read(currentHigh), Read(high)),
                Assign(lowOffset, Multiply(Unsigned(Read(rank)), U64((ulong)context.LowerBitCount))),
                Assign(lowWord, Int(ShiftRight(Read(lowOffset), I32(6)))),
                Assign(bit, Int(And(Read(lowOffset), U64(63)))),
                Assign(currentLow, ShiftRight(Index(Read(lower), Read(lowWord)), Read(bit))),
                If(Greater(Add(Read(bit), I32(context.LowerBitCount)), I32(64)),
                    Assign(currentLow, Or(Read(currentLow), ShiftLeft(Index(Read(lower), Add(Read(lowWord), I32(1))), Subtract(I32(64), Read(bit)))))),
                Assign(currentLow, And(Read(currentLow), U64(context.LowerMask))),
                If(Equal(Read(currentLow), target), Return(I32(0))),
                If(Greater(Read(currentLow), target), Return(I32(-1)))));
        }

        loop.Add(Assign(window, And(Read(window), Subtract(Read(window), U64(1)))));
        loop.Add(Assign(rank, Add(Read(rank), I64(1))));
        loop.Add(If(GreaterOrEqual(Unsigned(Read(rank)), U64(lowering.Config.ItemCount)), Return(I32(-1))));
        body.Add(While(new IrBoolLiteral(true), loop.ToArray()));
        body.Add(Return(I32(-1)));
        return body;
    }
}