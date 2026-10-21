using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Generator.Ir.Tests;

public class SwitchDetectionVisitorTests
{
    [Fact]
    public void MatchesConsecutiveReturningCasesBeforeTheMiss()
    {
        IrVariableExpression key = new IrVariableExpression(new IrVariableId(0, IrType.I32));
        IrBlock body = Block(If(Equal(key, I32(10)), Return(I32(0))),
            If(Equal(key, I32(20)), Return(I32(1))),
            If(Equal(key, I32(30)), Return(I32(2))),
            Return(I32(-1)));
        SwitchDetectionVisitor dispatch = new SwitchDetectionVisitor(["key"]);
        dispatch.Visit(body);

        Assert.Equal(3, dispatch.Cases.Count);
        Assert.Equal("key", dispatch.VariableName);
        Assert.Equal(IrType.I32, dispatch.Type);
        Assert.Equal(new IrLiteral[] { I32(10), I32(20), I32(30) }, dispatch.Cases.Select(item => item.Label));
        Assert.Equal(new IrExpression[] { I32(0), I32(1), I32(2) }, dispatch.Cases.Select(item => item.Result));
    }

    [Fact]
    public void VisitorCanMatchSuccessiveGroupsOnDifferentVariables()
    {
        IrVariableExpression first = new IrVariableExpression(new IrVariableId(0, IrType.I32));
        IrVariableExpression second = new IrVariableExpression(new IrVariableId(1, IrType.I32));
        IrBlock body = Block(If(Equal(first, I32(1)), Return(I32(10))),
            If(Equal(first, I32(2)), Return(I32(20))),
            If(Equal(first, I32(3)), Return(I32(30))),
            If(Equal(second, I32(1)), Return(I32(40))),
            If(Equal(second, I32(2)), Return(I32(50))),
            If(Equal(second, I32(3)), Return(I32(60))));
        SwitchDetectionVisitor dispatch = new SwitchDetectionVisitor(["first", "second"]);
        dispatch.Visit(body);
        Assert.Equal("first", dispatch.VariableName);
        Assert.Equal(3, dispatch.Cases.Count);

        dispatch.Visit(body, dispatch.Cases.Count);
        Assert.Equal("second", dispatch.VariableName);
        Assert.Equal(new IrExpression[] { I32(40), I32(50), I32(60) }, dispatch.Cases.Select(item => item.Result));
    }
}