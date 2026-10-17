using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrCallTests
{
    [Fact]
    public void AssigningAParameterDoesNotChangeTheCallersVariable()
    {
        IrProgramBuilder builder = new IrProgramBuilder();
        IrFunctionBuilder helper = builder.Function("Change", IrType.I32);
        IrVariableId parameter = helper.Parameter("value", IrType.I32);
        helper.Define(Block(Assign(parameter, Constant(42)), Return(Read(parameter))));
        IrFunctionBuilder caller = builder.Function("Caller", IrType.I32);
        IrVariableId local = caller.Local("local", IrType.I32);
        IrVariableId result = caller.Local("result", IrType.I32);
        caller.Define(Block(Assign(local, Constant(7)),
            Assign(result, Call(helper.Id, Read(local))),
            ReturnIf(Equal(Read(result), Constant(42)), Read(local), Constant(-1))));
        Assert.Equal(Constant(7), new IrInterpreter(builder.Build()).Invoke(caller.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefinedAndExternalCallsBindArgumentsInParameterOrder(bool useDefinedFunction)
    {
        IrProgramBuilder builder = new IrProgramBuilder();
        IrFunctionBuilder caller = builder.Function("Caller", IrType.I32);
        IrFunctionId combine = builder.ExternalFunction("combine", new IrFunctionSignature(IrType.I32, [IrType.I32, IrType.I32]));
        IrFunctionBuilder helper = builder.Function("Forward", IrType.I32);
        IrVariableId left = helper.Parameter("left", IrType.I32);
        IrVariableId right = helper.Parameter("right", IrType.I32);
        helper.Define(Block(Return(Call(combine, Read(left), Read(right)))));

        // The caller is declared before Forward, so defined calls also cover forward references.
        caller.Define(Block(Return(Call(useDefinedFunction ? helper.Id : combine, Constant(9), Constant(2)))));
        IrInterpreter interpreter = new IrInterpreter(builder.Build(), (_, arguments) =>
        {
            int first = ((IrI32Literal)arguments[0]).Value;
            int second = ((IrI32Literal)arguments[1]).Value;
            return Constant((first * 10) + second);
        });
        Assert.Equal(Constant(92), interpreter.Invoke(caller.Id));
    }
}