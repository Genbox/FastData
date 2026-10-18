using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Execution;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Building.IrFactory;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests.Code;

internal static class LookupTestPrograms
{
    /// <summary>Evaluates an expression built from a lookup builder whose Find always misses.</summary>
    public static IrLiteral Evaluate(IrType keyType, IrType resultType, Func<FastDataProgramBuilder, IrExpression> expression,
                                     IrTextEncoding encoding = IrTextEncoding.Utf16, Func<IrProgramBuilder, IrFunctionId>? fold = null,
                                     IrBuiltinOperation supportedBuiltins = IrBuiltins.All)
    {
        FastDataProgramBuilder builder = new FastDataProgramBuilder(keyType, encoding, fold, supportedBuiltins);
        IrBlock body = Block(Return(Constant(-1)));
        IrFunctionBuilder function = builder.Program.Function("Evaluate", resultType);
        function.Define(Block(Return(expression(builder))));
        IrProgram program = builder.Build(body).Program;
        return new IrInterpreter(program, IrBuiltinEvaluator.CreateHandler(program)).Invoke(function.Id);
    }
}