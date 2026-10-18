using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Generator.Ir.Lowering.LoweringHelpers;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal sealed class HashTableCompact<TKey, TValue>(LoweringContext<TKey, TValue> lowering)
{
    public IEnumerable<IrStatement> Lower(HashTableCompactContext<TKey, TValue> context)
    {
        Func<IrExpression, IrExpression, IrExpression> match = lowering.HashEntries(context.Entries.Select(static x => x.Key).ToArray(),
            context.StoreHashCode ? context.Entries.Select(static x => x.Hash).ToArray() : null,
            context.Values);
        (IrDataId starts, IrType type) = lowering.Storage.Unsigned("bucketStarts", context.BucketStarts, IrType.U32);
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId bucket = lowering.Builder.FindLocal("bucket", IrType.I32);
        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);
        IrVariableId end = lowering.Builder.FindLocal("end", IrType.I32);

        return
        [
            Assign(hash, lowering.Expressions.Hash()),
            Assign(bucket, Int(Mod(Read(hash), U64((ulong)context.BucketStarts.Length - 1)), IrType.U64)),
            Assign(index, Int(Index(Read(starts), Read(bucket)), type)),
            Assign(end, Int(Index(Read(starts), Add(Read(bucket), I32(1))), type)),
            While(Less(Read(index), Read(end)),
                If(match(Read(index), Read(hash)), Return(Read(index))),
                Increment(index)),
            Return(I32(-1))
        ];
    }
}