using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class BloomFilter<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public IEnumerable<IrStatement> Lower(BloomFilterContext context)
    {
        IrDataId bits = lowering.Builder.Program.AddData("bloom", IrClrConverter.FromValue(context.BitSet));
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId mask = lowering.Builder.FindLocal("mask", IrType.U64);
        IrExpression index = Int(Mod(Read(hash), U64((ulong)context.BitSet.Length)), IrType.U64);

        return
        [
            Assign(hash, lowering.Expressions.Hash()),
            Assign(mask,
                Or(ShiftLeft(U64(1), Int(And(Read(hash), U64(63)), IrType.U64)),
                    ShiftLeft(U64(1), Int(And(ShiftRight(Read(hash), I32(8)), U64(63)), IrType.U64)))),
            If(Equal(And(Index(Read(bits), index), Read(mask)), Read(mask)), Return(I32(0))),
            Return(I32(-1))
        ];
    }
}