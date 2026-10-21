using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Generator.Ir.Harness;

internal static class HarnessBuilder
{
    internal static HarnessCall Call(HarnessOperation operation, params IrExpression[] arguments) => new HarnessCall(operation, operation switch
    {
        HarnessOperation.Contains or HarnessOperation.LookupMatches or HarnessOperation.LookupMiss => IrType.Bool,
        HarnessOperation.KeyCount or HarnessOperation.ValueCount or HarnessOperation.KeySpanCount or HarnessOperation.ValueSpanCount => IrType.I32,
        HarnessOperation.Clock or HarnessOperation.Consume or HarnessOperation.Report => IrType.I64,
        HarnessOperation.Elapsed => IrType.F64,
        HarnessOperation.Opaque => arguments[0].Type,
        _ => throw new InvalidOperationException("The operation requires an explicit result type.")
    }, arguments);

    internal static HarnessCall Call(HarnessOperation operation, IrType type, params IrExpression[] arguments) => new HarnessCall(operation, type, arguments);

    internal static void AddLoop(List<IrStatement> body, IrVariableId index, int count, IEnumerable<IrStatement> statements)
    {
        body.Add(Assign(index, I32(0)));
        List<IrStatement> loop = statements.ToList();
        loop.Add(Increment(index));
        body.Add(new IrLoopStatement(Less(Read(index), I32(count)), new IrBlock(loop)));
    }
}