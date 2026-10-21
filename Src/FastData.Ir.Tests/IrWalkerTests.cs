using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Visiting;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrWalkerTests
{
    [Fact]
    public void WalkerTraversesBodiesAndUnpackedStorageInSourceOrder()
    {
        IrRecordId recordId = new IrRecordId(0);
        IrRecordLiteral record = new IrRecordLiteral(recordId, [I32(3), I32(4)]);
        IrArrayLiteral unpacked = new IrArrayLiteral(record.Type, [record]);
        IrPackedArrayLiteral<int> packed = new IrPackedArrayLiteral<int>([10, 11]);
        IrFunction function = new IrFunction("Read", IrType.I32, [], [], Block(If(Bool(true), Return(Field(Index(unpacked, I32(0)), new IrFieldId(recordId, 0, IrType.I32)))), Return(new IrIndexExpression(packed, I32(1)))));
        IntegerCollector walker = new IntegerCollector();
        walker.Visit(function);

        Assert.Equal(new[] { 3, 4, 0, 1 }, walker.Values);
        Assert.Equal(1, walker.PackedArrays);
    }

    [Fact]
    public void WalkerTraversesNestedExtensionOperandsInSourceOrder()
    {
        IrExpression expression = new Extension([I32(1), Add(I32(2), new Extension([I32(3), I32(4)]))]);
        IntegerCollector walker = new IntegerCollector();
        walker.Visit(expression);

        Assert.Equal(new[] { 1, 2, 3, 4 }, walker.Values);
    }

    private sealed class Extension(IReadOnlyList<IrExpression> children) : IrExtensionExpression
    {
        public IrType Type => IrType.I32;
        public IReadOnlyList<IrExpression> Children { get; } = children;
    }

    private sealed class IntegerCollector : IrWalker
    {
        public List<int> Values { get; } = [];
        public int PackedArrays { get; private set; }

        public override void VisitLiteral(IrLiteral literal)
        {
            if (literal is IrI32Literal value)
                Values.Add(value.Value);
            else if (literal is IrPackedArrayLiteral<int>)
                PackedArrays++;

            base.VisitLiteral(literal);
        }
    }
}