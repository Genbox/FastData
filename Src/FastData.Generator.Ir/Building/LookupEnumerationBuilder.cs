using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Building;

internal sealed class LookupEnumerationBuilder(FastDataProgramBuilder builder)
{
    // Array-shaped cursors: callbacks receive I32 slots; the public cursor uses I64 positions.
    public void AddCursor(int count, Func<IrExpression, IrExpression> keyAt, Func<IrExpression, IrExpression>? valueAt, Func<IrExpression, IrExpression>? occupied = null)
    {
        IrFunctionId next = AddNext(count - 1, occupied == null ? null : position => occupied(Convert(IrType.I32, position)));
        AddEnumeration(next, position => keyAt(Convert(IrType.I32, position)), valueAt == null ? null : position => valueAt(Convert(IrType.I32, position)));
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
        IrFunctionId keyId = AddAccessor("KeyAt", builder.KeyParameterId.Type, keyAt);
        IrFunctionId? valueId = builder.Values != null && valueAt != null ? AddAccessor("ValueAt", builder.Values.Type, valueAt) : null;
        builder.Enumeration = new LookupEnumeration(next, keyId, valueId);
    }

    private IrFunctionId AddAccessor(string name, IrType type, Func<IrExpression, IrExpression> elementAt)
    {
        IrFunctionBuilder function = builder.Program.Function(name, type);
        IrVariableId position = function.Parameter("position", IrType.I64);
        function.Define(Block(Return(elementAt(Read(position)))));
        return function.Id;
    }
}