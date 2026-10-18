using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Building;

internal sealed class LookupEnumerationBuilder<TValue>(FastDataProgramBuilder builder, IrType keyType)
{
    // Array-shaped cursors: positions are slots 0..count-1.
    public void AddCursor(int count, Func<IrExpression, IrExpression> keyAt, Func<IrExpression, IrExpression>? valueAt)
    {
        IrFunctionId next = AddNext(count - 1);
        AddEnumeration(next, position => keyAt(Convert(IrType.I32, position, IrType.I64)), valueAt == null ? null : position => valueAt(Convert(IrType.I32, position, IrType.I64)));
    }

    // Next(position) returns the first position after the given one that is at most last and passes the optional occupancy test, or -1.
    public IrFunctionId AddNext(long last, Func<IrExpression, IrExpression>? occupied = null)
    {
        IrFunctionBuilder next = builder.Program.Function("Next", IrType.I64);
        IrVariableId position = next.Parameter("position", IrType.I64);

        if (occupied == null)
        {
            next.Define(Block(If(Less(Read(position), I64(last)), Return(Add(Read(position), I64(1)))), Return(I64(-1))));
            return next.Id;
        }

        next.Define(Block(Assign(position, Add(Read(position), I64(1))),
            While(LessOrEqual(Read(position), I64(last)),
                If(occupied(Read(position)), Return(Read(position))),
                Assign(position, Add(Read(position), I64(1)))),
            Return(I64(-1))));
        return next.Id;
    }

    public void AddEnumeration(IrFunctionId next, Func<IrExpression, IrExpression> keyAt, Func<IrExpression, IrExpression>? valueAt)
    {
        IrFunctionBuilder keyFunction = builder.Program.Function("KeyAt", keyType);
        IrVariableId keyPosition = keyFunction.Parameter("position", IrType.I64);
        keyFunction.Define(Block(Return(keyAt(Read(keyPosition)))));

        IrFunctionId? valueId = null;

        if (builder.Values != null && valueAt != null)
        {
            IrFunctionBuilder valueFunction = builder.Program.Function("ValueAt", IrClrConverter.FromClrType(typeof(TValue)));
            IrVariableId valuePosition = valueFunction.Parameter("position", IrType.I64);
            valueFunction.Define(Block(Return(valueAt(Read(valuePosition)))));
            valueId = valueFunction.Id;
        }

        builder.Enumeration = new LookupEnumeration(next, keyFunction.Id, valueId);
    }
}