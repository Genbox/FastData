using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class RrrBitVector<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public IEnumerable<IrStatement> Lower(RrrBitVectorContext context)
    {
        IrDataId classes = lowering.Builder.Program.AddData("classes", IrClrConverter.FromValue(context.Classes));
        (IrDataId offsets, IrType offsetType) = lowering.Storage.Unsigned("offsets", context.Offsets, IrType.U32);

        // RRR blocks use combinatorial ranks. Precompute the small binomial table once during generation.
        int width = context.BlockSize + 1;
        uint[] combinations = new uint[width * width];

        for (int n = 0; n < width; n++)
        {
            combinations[n * width] = 1;
            combinations[(n * width) + n] = 1;
            for (int k = 1; k < n; k++)
                combinations[(n * width) + k] = combinations[(((n - 1) * width) + k) - 1] + combinations[((n - 1) * width) + k];
        }

        IrDataId binomial = lowering.Builder.Program.AddData("binomial", IrClrConverter.FromValue(combinations));
        IrVariableId normalized = lowering.Builder.FindLocal("normalized", IrType.U64);
        IrVariableId block = lowering.Builder.FindLocal("block", IrType.I32);
        IrVariableId target = lowering.Builder.FindLocal("target", IrType.I32);
        IrVariableId remaining = lowering.Builder.FindLocal("remaining", IrType.I32);
        IrVariableId rank = lowering.Builder.FindLocal("rank", IrType.U32);
        IrVariableId bit = lowering.Builder.FindLocal("bit", IrType.I32);
        IrVariableId combination = lowering.Builder.FindLocal("combination", IrType.U32);
        IrExpression mapped = Unsigned(lowering.Key, lowering.KeyType);

        if (lowering.KeyType.IsSignedIntegral)
        {
            ulong mask = lowering.KeyType.BitWidth == 64 ? ulong.MaxValue : (1UL << lowering.KeyType.BitWidth) - 1;
            mapped = Xor(And(mapped, U64(mask)), U64(1UL << (lowering.KeyType.BitWidth - 1)));
        }

        return
        [
            Assign(normalized, Subtract(mapped, U64(context.MinValue))),
            If(Greater(Read(normalized), U64(context.MaxValue - context.MinValue)), Return(I32(-1))),
            Assign(block, Int(Divide(Read(normalized), U64((ulong)context.BlockSize)), IrType.U64)),
            Assign(target, Int(Mod(Read(normalized), U64((ulong)context.BlockSize)), IrType.U64)),
            Assign(remaining, Int(Index(Read(classes), Read(block)), IrType.U8)),
            Assign(rank, Convert(IrType.U32, Index(Read(offsets), Read(block)), offsetType)),
            Assign(bit, I32(context.BlockSize - 1)),
            While(GreaterOrEqual(Read(bit), Read(target)),
                If(Equal(Read(remaining), I32(0)), Return(I32(-1))),
                Assign(combination, Index(Read(binomial), Add(Multiply(Read(bit), I32(width)), Read(remaining)))),
                IfElse(GreaterOrEqual(Read(rank), Read(combination)),
                    Block(Assign(rank, Subtract(Read(rank), Read(combination))),
                        Assign(remaining, Subtract(Read(remaining), I32(1))),
                        If(Equal(Read(bit), Read(target)), Return(I32(0)))),
                    Block(If(Equal(Read(bit), Read(target)), Return(I32(-1))))),
                Assign(bit, Subtract(Read(bit), I32(1)))),
            Return(I32(-1))
        ];
    }
}