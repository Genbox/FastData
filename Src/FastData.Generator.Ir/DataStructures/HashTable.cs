using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class HashTable<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, HashTableContext<TKey, TValue> context)
    {
        HashTableEntry<TKey>[] entries = context.Entries;
        ReadOnlySpan<TValue> values = context.Values.Span;
        Type nextClr = lowering.Storage.SignedType(entries, static x => x.Next);
        Type hashClr = context.StoreHashCode ? lowering.Storage.UnsignedType(entries, static x => x.Hash, typeof(ulong)) : typeof(ulong);
        IrType nextType = IrClrConverter.FromClrType(nextClr);
        IrType hashType = IrClrConverter.FromClrType(hashClr);

        IrRecordId record = lowering.Builder.Program.DeclareRecord("Entry");
        IrFieldId keyField = lowering.Builder.Program.AddField(record, "Key", lowering.KeyType);
        IrFieldId nextField = lowering.Builder.Program.AddField(record, "Next", nextType);
        IrFieldId? hashField = context.StoreHashCode ? lowering.Builder.Program.AddField(record, "HashCode", hashType) : null;
        IrType? valueType = values.Length > 0 ? lowering.ValueType() : null;
        IrFieldId? valueField = valueType != null ? lowering.Builder.Program.AddField(record, "Value", valueType) : null;

        IrLiteral[] records = new IrLiteral[entries.Length];
        int fieldCount = 2 + (hashField != null ? 1 : 0) + (valueField != null ? 1 : 0);

        for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
        {
            IrLiteral[] fields = new IrLiteral[fieldCount];
            fields[0] = IrClrConverter.FromValue(entries[entryIndex].Key);
            fields[1] = IrClrConverter.FromValue(IntegralValueConverter.ConvertChecked(entries[entryIndex].Next, nextClr));
            int fieldIndex = 2;
            if (hashField != null)
                fields[fieldIndex++] = IrClrConverter.FromValue(IntegralValueConverter.ConvertChecked(entries[entryIndex].Hash, hashClr));
            if (valueType != null)
                fields[fieldIndex] = IrClrConverter.FromValue(values[entryIndex], valueType);

            records[entryIndex] = new IrRecordLiteral(record, fields);
        }

        IrDataId table = lowering.Builder.Program.AddData("entries", new IrArrayLiteral(IrType.Record(record), records));

        IrDataId buckets = lowering.Storage.NonNegative("buckets", context.Buckets);
        if (valueField != null)
            lowering.Builder.Values = new LookupValues(table, valueField);
        lowering.Enumeration.AddCursor(entries.Length,
            position => Field(Index(Read(table), position), keyField),
            valueField == null ? null : position => Field(Index(Read(table), position), valueField.Value));

        IrVariableId hash = lowering.Builder.FindLocal("hash", IrType.U64);
        IrVariableId index = lowering.Builder.FindLocal("i", IrType.I32);
        IrExpression bucket = Convert(IrType.I32, Binary(BinaryOperation.Modulo, Read(hash), U64((ulong)context.Buckets.Length)));
        IrExpression match = lowering.Builder.Keys.Equal(Field(Index(Read(table), Read(index)), keyField), lowering.Key);
        if (hashField != null)
            match = AndAlso(Equal(Convert(IrType.U64, Field(Index(Read(table), Read(index)), hashField.Value)), Read(hash)), match);

        return
        [
            Assign(hash, lowering.Expressions.Hash()),
            Assign(index, Subtract(Convert(IrType.I32, Index(Read(buckets), bucket)), I32(1))),
            While(GreaterOrEqual(Read(index), I32(0)),
                If(match, Return(Read(index))),
                Assign(index, Convert(IrType.I32, Field(Index(Read(table), Read(index)), nextField)))),
            Return(I32(-1))
        ];
    }
}