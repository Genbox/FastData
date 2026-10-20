using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.DataStructures;

internal static class Conditional<TKey, TValue>
{
    public static IReadOnlyList<IrStatement> Lower(LoweringContext<TKey, TValue> lowering, ReadOnlyMemory<TKey> keyMemory, ReadOnlyMemory<TValue> values)
    {
        TKey[] keys = GetArray(keyMemory);
        IrLiteral[] literals = keys.Select(static key => IrClrConverter.FromValue(key)).ToArray();
        lowering.AddValues(values);
        lowering.Enumeration.AddCursor(keys.Length,
            position => KeyAtPosition(lowering, literals, position),
            lowering.Builder.Values == null ? null : lowering.ValueAt);

        List<IrStatement> statements = new List<IrStatement>(keys.Length + 1);
        for (int i = 0; i < literals.Length; i++)
            statements.Add(If(lowering.Builder.Keys.Equal(lowering.Key, literals[i]), Return(I32(i))));
        statements.Add(Return(I32(-1)));
        return statements;
    }

    // Conditional structures store no key array, so enumeration selects the literal by position.
    private static IrFunctionCallExpression KeyAtPosition(LoweringContext<TKey, TValue> lowering, IrLiteral[] literals, IrExpression position)
    {
        IrFunctionBuilder function = lowering.Builder.Program.Function("KeyLiteral", lowering.KeyType);
        IrVariableId index = function.Parameter("position", IrType.I32);
        List<IrStatement> statements = new List<IrStatement>(literals.Length);
        for (int i = 0; i < literals.Length - 1; i++)
            statements.Add(If(Equal(Read(index), I32(i)), Return(literals[i])));
        statements.Add(Return(literals[literals.Length - 1]));
        function.Define(new IrBlock(statements));
        return Call(function.Id, position);
    }
}