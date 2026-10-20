using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Helpers;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class Pgm<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, PgmContext<TKey, TValue> context)
    {
        IrDataId keys = lowering.ArrayLookup(GetArray(context.Keys), context.Values);
        IrDataId? segmentKeys = null;
        IrDataId? slopes = null;
        IrDataId? intercepts = null;

        if (context.SegmentCount > 1)
        {
            segmentKeys = lowering.Builder.Program.AddData("segmentKeys", IrClrConverter.FromValue(context.Segments.Select(static s => s.Key).ToArray()));
            slopes = lowering.Builder.Program.AddData("segmentSlopes",
                IrClrConverter.FromValue(context.Segments.Select(static s => NumericPrediction.Coefficient(s.Slope)).ToArray()));
            intercepts = lowering.Storage.Signed("segmentIntercepts", context.Segments, static s => s.Intercept);
        }

        IrExpression SegmentKey(IrExpression index) => segmentKeys is {} data ? Index(Read(data), index) : IrClrConverter.FromValue(context.Segments[0].Key);
        IrExpression Slope(IrExpression index) => slopes is {} data ? Index(Read(data), index) : U64(NumericPrediction.Coefficient(context.Segments[0].Slope));
        IrExpression Intercept(IrExpression index) => intercepts is {} data ? Int(Index(Read(data), index)) : I32(context.Segments[0].Intercept);

        IrFunctionBuilder evaluate = lowering.Builder.Program.Function("EvaluateSegment", IrType.I32);
        IrVariableId segment = evaluate.Parameter("segment", IrType.I32);
        IrVariableId key = evaluate.Parameter("key", lowering.KeyType);
        IrVariableId product = evaluate.Local("product", IrType.U64);
        IrVariableId max = evaluate.Local("max", IrType.I32);
        IrExpression intercept = Intercept(Read(segment));
        evaluate.Define(Block(Assign(product, lowering.MultiplyHigh(Slope(Read(segment)),
                Subtract(lowering.Builder.Keys.Coordinate(Read(key)), lowering.Builder.Keys.Coordinate(SegmentKey(Read(segment)))))),
            Assign(max, Subtract(I32(context.Keys.Length), intercept)),
            If(Greater(Read(product), Unsigned(Read(max))), Return(I32(context.Keys.Length))),
            Return(Add(Int(Read(product)), intercept))));

        IrVariableId segmentIndex = lowering.Builder.FindLocal("segment", IrType.I32);
        IrVariableId position = lowering.Builder.FindLocal("position", IrType.I32);
        IrVariableId low = lowering.Builder.FindLocal("low", IrType.I32);
        IrVariableId high = lowering.Builder.FindLocal("high", IrType.I32);
        IrVariableId middle = lowering.Builder.FindLocal("middle", IrType.I32);
        IrExpression nextIntercept = Intercept(Add(Read(segmentIndex), I32(1)));
        List<IrStatement> body =
        [
            If(Not(AndAlso(GreaterOrEqual(lowering.Key, Index(Read(keys), I32(0))),
                    LessOrEqual(lowering.Key, Index(Read(keys), I32(context.Keys.Length - 1))))),
                Return(I32(-1)))
        ];

        void Predict()
        {
            body.Add(Assign(position, Call(evaluate.Id, Read(segmentIndex), lowering.Key)));
            if (context.SegmentCount > 1)
                body.Add(If(Greater(Read(position), nextIntercept), Assign(position, nextIntercept)));
        }

        void SearchSegments() => body.Add(While(Less(Read(segmentIndex), Read(high)),
            Assign(middle, Add(Read(segmentIndex), HalveNonNegative(Subtract(Read(high), Read(segmentIndex))))),
            IfElse(LessOrEqual(SegmentKey(Add(Read(middle), I32(1))), lowering.Key),
                Block(Assign(segmentIndex, Add(Read(middle), I32(1)))),
                Block(Assign(high, Read(middle))))));

        if (context.SegmentCount == 1)
            body.Add(Assign(segmentIndex, I32(0)));
        else if (context.EpsilonRecursive > 0 && context.LevelsOffsets.Length > 2)
        {
            body.Add(Assign(segmentIndex, I32(context.LevelsOffsets[context.LevelsOffsets.Length - 2])));

            for (int level = context.LevelsOffsets.Length - 3; level >= 0; level--)
            {
                int begin = context.LevelsOffsets[level];
                int size = context.LevelsOffsets[level + 1] - begin - 1;
                int epsilon = context.EpsilonRecursive + 1;
                Predict();
                body.Add(Assign(low, Subtract(Read(position), I32(epsilon + 1))));
                body.Add(If(Less(Read(low), I32(0)), Assign(low, I32(0))));
                body.Add(Assign(segmentIndex, Add(I32(begin), Read(low))));

                if (epsilon > 64)
                {
                    body.Add(Assign(high, Add(Read(position), I32(epsilon + 2))));
                    body.Add(If(Greater(Read(high), I32(size)), Assign(high, I32(size))));
                    body.Add(Assign(high, Add(I32(begin), Read(high))));
                    SearchSegments();
                }
                else
                {
                    body.Add(While(AndAlso(Less(Add(Read(segmentIndex), I32(1)), I32(begin + size)),
                            LessOrEqual(SegmentKey(Add(Read(segmentIndex), I32(1))), lowering.Key)),
                        Increment(segmentIndex)));
                }
            }
        }
        else
        {
            body.Add(Assign(segmentIndex, I32(0)));
            body.Add(Assign(high, I32(context.SegmentCount - 1)));
            SearchSegments();
        }

        Predict();
        body.Add(Assign(low, Subtract(Read(position), I32(context.Epsilon + 1))));
        body.Add(If(Less(Read(low), I32(0)), Assign(low, I32(0))));
        body.Add(Assign(high, Add(Read(position), I32(context.Epsilon + 3))));
        body.Add(If(Greater(Read(high), I32(context.Keys.Length)), Assign(high, I32(context.Keys.Length))));
        body.Add(While(Less(Read(low), Read(high)),
            Assign(middle, Add(Read(low), HalveNonNegative(Subtract(Read(high), Read(low))))),
            If(Equal(Index(Read(keys), Read(middle)), lowering.Key), Return(Read(middle))),
            IfElse(Less(Index(Read(keys), Read(middle)), lowering.Key),
                Block(Assign(low, Add(Read(middle), I32(1)))),
                Block(Assign(high, Read(middle))))));
        body.Add(Return(I32(-1)));
        return body;
    }
}