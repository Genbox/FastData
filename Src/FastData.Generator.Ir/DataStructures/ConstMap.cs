using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class ConstMap<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public IEnumerable<IrStatement> Lower(ConstMapContext<TKey, TValue> context)
    {
        IrDataId keys = lowering.ArrayLookup(GetArray(context.Keys), context.Values);
        (IrDataId data, IrType type) = lowering.Storage.Unsigned("cells", context.Data, IrType.U32);
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId h0 = lowering.Builder.FindLocal("h0", IrType.U64);
        IrVariableId candidate = lowering.Builder.FindLocal("candidate", IrType.U64);
        IrExpression h1 = Xor(Add(Read(h0), U64(context.SegmentLength)), And(ShiftRight(Read(hash), I32(18)), U64(context.SegmentLength - 1)));
        IrExpression h2 = Xor(Add(Read(h0), U64(2UL * context.SegmentLength)), And(Read(hash), U64(context.SegmentLength - 1)));
        IrExpression Cell(IrExpression index) => Unsigned(Index(Read(data), Int(index, IrType.U64)), type);

        return
        [
            Assign(hash, Add(lowering.Expressions.Hash(), U64(context.Seed))),
            Assign(hash, Multiply(Xor(Read(hash), ShiftRight(Read(hash), I32(33))), U64(0xff51afd7ed558ccd))),
            Assign(hash, Multiply(Xor(Read(hash), ShiftRight(Read(hash), I32(33))), U64(0xc4ceb9fe1a85ec53))),
            Assign(hash, Xor(Read(hash), ShiftRight(Read(hash), I32(33)))),
            Assign(h0, lowering.MultiplyHigh(Read(hash), U64(context.SegmentCountLength))),
            Assign(candidate, Xor(Xor(Cell(Read(h0)), Cell(h1)), Cell(h2))),
            If(GreaterOrEqual(Read(candidate), U64((ulong)context.Keys.Length)), Return(I32(-1))),
            If(lowering.Builder.Keys.Equal(Index(Read(keys), Int(Read(candidate), IrType.U64)), lowering.Key), Return(Int(Read(candidate), IrType.U64))),
            Return(I32(-1))
        ];
    }
}