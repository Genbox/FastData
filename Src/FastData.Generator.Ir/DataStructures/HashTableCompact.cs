using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class HashTableCompact<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, HashTableCompactContext<TKey, TValue> context)
    {
        Func<IrExpression, IrExpression, IrExpression> match = lowering.HashEntries(context.Entries.Select(static x => x.Key).ToArray(),
            context.StoreHashCode ? context.Entries.Select(static x => x.Hash).ToArray() : null,
            context.Values);
        IrDataId starts = lowering.Storage.Unsigned("bucketStarts", context.BucketStarts);
        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId bucket = lowering.Builder.FindLocal("bucket", IrType.I32);
        IrVariableId index = lowering.Builder.FindLocal("index", IrType.I32);
        IrVariableId end = lowering.Builder.FindLocal("end", IrType.I32);

        return
        [
            Assign(hash, lowering.Expressions.Hash()),
            Assign(bucket, Int(Mod(Read(hash), U64((ulong)context.BucketStarts.Length - 1)))),
            Assign(index, Int(Index(Read(starts), Read(bucket)))),
            Assign(end, Int(Index(Read(starts), Add(Read(bucket), I32(1))))),
            While(Less(Read(index), Read(end)),
                If(match(Read(index), Read(hash)), Return(Read(index))),
                Increment(index)),
            Return(I32(-1))
        ];
    }
}