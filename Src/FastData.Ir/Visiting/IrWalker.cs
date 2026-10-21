using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Visiting;

public class IrWalker
{
    public virtual void Visit(IrExpression expression)
    {
        switch (expression)
        {
            case IrLiteral literal: VisitLiteral(literal); break;
            case IrVariableExpression variable: VisitVariable(variable); break;
            case IrDataExpression data: VisitData(data); break;
            case IrIndexExpression index: VisitIndex(index); break;
            case IrFieldExpression field: VisitField(field); break;
            case IrBinaryExpression binary: VisitBinary(binary); break;
            case IrLogicalExpression logical: VisitLogical(logical); break;
            case IrConversionExpression conversion: VisitConversion(conversion); break;
            case IrFunctionCallExpression call: VisitCall(call); break;
            case IrExtensionExpression extension: VisitExtension(extension); break;
        }
    }

    public virtual void Visit(IrStatement statement)
    {
        switch (statement)
        {
            case IrAssignmentStatement assignment: VisitAssignment(assignment); break;
            case IrReturnStatement result: VisitReturn(result); break;
            case IrIfStatement conditional: VisitIf(conditional); break;
            case IrLoopStatement loop: VisitLoop(loop); break;
            case IrBreakStatement stop: VisitBreak(stop); break;
        }
    }

    public virtual void Visit(IrBlock block)
    {
        foreach (IrStatement statement in block.Statements)
            Visit(statement);
    }

    public virtual void Visit(IrFunction function) => Visit(function.Body);

    public virtual void VisitLiteral(IrLiteral literal)
    {
        switch (literal)
        {
            case IrArrayLiteral array:
                foreach (IrLiteral element in array.Elements)
                    Visit(element);
                break;
            case IrRecordLiteral record:
                foreach (IrLiteral field in record.Fields)
                    Visit(field);
                break;
        }
    }

    public virtual void VisitVariable(IrVariableExpression variable) {}

    public virtual void VisitData(IrDataExpression data) {}

    public virtual void VisitIndex(IrIndexExpression index)
    {
        Visit(index.Collection);
        Visit(index.Index);
    }

    public virtual void VisitField(IrFieldExpression field) => Visit(field.Receiver);

    public virtual void VisitBinary(IrBinaryExpression binary)
    {
        Visit(binary.Left);
        Visit(binary.Right);
    }

    public virtual void VisitLogical(IrLogicalExpression logical)
    {
        Visit(logical.Left);
        Visit(logical.Right);
    }

    public virtual void VisitConversion(IrConversionExpression conversion) => Visit(conversion.Operand);

    public virtual void VisitCall(IrFunctionCallExpression call)
    {
        foreach (IrExpression argument in call.Arguments)
            Visit(argument);
    }

    public virtual void VisitExtension(IrExtensionExpression extension)
    {
        foreach (IrExpression child in extension.Children)
            Visit(child);
    }

    public virtual void VisitAssignment(IrAssignmentStatement assignment) => Visit(assignment.Value);

    public virtual void VisitReturn(IrReturnStatement result) => Visit(result.Value);

    public virtual void VisitIf(IrIfStatement conditional)
    {
        Visit(conditional.Condition);
        Visit(conditional.IfTrue);

        if (conditional.IfFalse is {} otherwise)
            Visit(otherwise);
    }

    public virtual void VisitLoop(IrLoopStatement loop)
    {
        Visit(loop.Condition);
        Visit(loop.Body);
    }

    public virtual void VisitBreak(IrBreakStatement statement) {}
}