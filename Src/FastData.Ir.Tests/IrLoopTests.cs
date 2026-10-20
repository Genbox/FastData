using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrLoopTests
{
    [Theory]
    [InlineData("normal", 3)]
    [InlineData("break", 0)]
    [InlineData("return", 0)]
    public void TailUpdatesRunAfterNormalCompletionButNotExits(string completion, int expected)
    {
        IrProgramBuilder builder = new IrProgramBuilder();
        IrFunctionBuilder function = builder.Function("Run", IrType.I32);
        IrVariableId index = function.Local("index", IrType.I32);
        IrStatement[] body = completion switch
        {
            "break" => [new IrBreakStatement()],
            "return" => [Return(Read(index))],
            _ => []
        };

        IrStatement[] loopBody = body.Append(Increment(index)).ToArray();
        function.Define(Block(Assign(index, Constant(0)),
            While(Less(Read(index), Constant(3)), loopBody),
            Return(Read(index))));

        Assert.Equal(Constant(expected), new IrInterpreter(builder.Build()).Invoke(function.Id));
    }

    [Fact]
    public void BranchesCompleteNormallyAndPreserveNestedLoopBreaks()
    {
        // A break exits its immediately enclosing loop.
        IrProgramBuilder builder = new IrProgramBuilder();
        IrFunctionBuilder function = builder.Function("Count", IrType.I32);
        IrVariableId iteration = function.Local("iteration", IrType.I32);
        IrVariableId count = function.Local("count", IrType.I32);
        IrIfStatement nestedLoop = If(Equal(Read(iteration), Constant(4)),
            While(Constant(true), new IrBreakStatement()));
        function.Define(Block(Assign(iteration, Constant(1)),
            Assign(count, Constant(0)),
            While(LessOrEqual(Read(iteration), Constant(5)),
                nestedLoop,
                Increment(count),
                Increment(iteration)),
            Return(Read(count))));

        Assert.Equal(new IrI32Literal(5), new IrInterpreter(builder.Build()).Invoke(function.Id));
    }
}