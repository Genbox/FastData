using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Common expression, statement, and block syntax for C-family backends.</summary>
public abstract class CStyleSourceEmitter(PreparedLookup lookup) : IrSourceEmitter(lookup)
{
    protected override string Parameter(PreparedDeclaration parameter) => $"{Type(parameter.Type)} {parameter.Name}";
    protected override void CloseFunction() => Writer.Close();
    protected override string Binary(BinaryOperation operation, string left, string right) => $"({left} {CStyleOperators.Symbol(operation)} {right})";
    protected override string Logical(LogicalOperation operation, string left, string right) => $"({left} {CStyleOperators.Symbol(operation)} {right})";
    protected override string StringComparison(BinaryOperation operation, string left, string right) => Binary(operation, left, right);
    protected override string Call(string name, string[] arguments) => $"{MemberReference(name)}({string.Join(", ", arguments)})";
    protected override void WriteAssignment(IrAssignmentStatement assignment) => Writer.Line($"{Variables[assignment.VariableId.Value]} = {Expression(assignment.Value)};");
    protected override void WriteReturn(IrReturnStatement result) => Writer.Line($"return {Expression(result.Value)};");
    protected override void WriteBreak() => Writer.Line("break;");
    protected override void WriteIf(IrExpression condition) => Writer.Line($"if ({Expression(condition)})");
    protected override void WriteElse() => Writer.Line("else");
    protected override void WriteLoop(IrExpression condition) => Writer.Line($"while ({Expression(condition)})");
    protected override void OpenBlock() => Writer.Open();
    protected override void CloseBlock() => Writer.Close();

    protected override void WriteSwitch(PreparedSwitch dispatch)
    {
        Writer.Line($"switch ({Expression(dispatch.Value)})");
        Writer.Open();

        foreach (PreparedSwitchCase item in dispatch.Cases)
            Writer.Line($"case {Literal(item.Label)}: return {Expression(item.Result)};");

        Writer.Close();
    }
}