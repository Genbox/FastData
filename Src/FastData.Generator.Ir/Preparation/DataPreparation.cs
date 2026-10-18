using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Preparation;

/// <summary>Names aggregate literals and orders their storage before the declarations that use them.</summary>
internal sealed class DataPreparation
{
    private readonly Dictionary<IrLiteral, string> _names = new Dictionary<IrLiteral, string>();
    private readonly List<PreparedData> _pending = [];
    private int _next;

    public string? Reference(IrLiteral literal) => _names.TryGetValue(literal, out string? name) ? name : null;

    public void Add(PreparedData data)
    {
        VisitChildren(data.Value);

        // The first name is the canonical reference; each explicitly named declaration is still emitted.
        if (data.Value is IrArrayLiteralBase or IrRecordLiteral)
            _names.TryAdd(data.Value, data.Name);
        _pending.Add(data);
    }

    public void Flush(Action<PreparedData> emit)
    {
        foreach (PreparedData data in _pending)
            emit(data);
        _pending.Clear();
    }

    public void Visit(IrBlock block)
    {
        foreach (IrStatement statement in block.Statements)
        {
            switch (statement)
            {
                case IrAssignmentStatement assignment:
                    Visit(assignment.Value);
                    break;
                case IrReturnStatement result:
                    Visit(result.Value);
                    break;
                case IrIfStatement conditional:
                    Visit(conditional.Condition);
                    Visit(conditional.IfTrue);
                    if (conditional.IfFalse is {} otherwise)
                        Visit(otherwise);
                    break;
                case IrLoopStatement loop:
                    Visit(loop.Condition);
                    Visit(loop.Body);
                    break;
                case PreparedSwitch dispatch:
                    foreach (PreparedSwitchCase item in dispatch.Cases)
                        Visit(item.Result);
                    break;
            }
        }
    }

    private void VisitChildren(IrLiteral literal)
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

    private void Visit(IrExpression expression)
    {
        switch (expression)
        {
            case IrLiteral literal when literal is IrArrayLiteralBase or IrRecordLiteral:
                if (!_names.ContainsKey(literal))
                    Add(new PreparedData(PreparedNames.Name("fdLiteral", _next++), literal));
                break;
            case IrIndexExpression index:
                Visit(index.Collection);
                Visit(index.Index);
                break;
            case IrFieldExpression field:
                Visit(field.Receiver);
                break;
            case IrLogicalExpression logical:
                Visit(logical.Left);
                Visit(logical.Right);
                break;
            case IrBinaryExpression binary:
                Visit(binary.Left);
                Visit(binary.Right);
                break;
            case IrConversionExpression conversion:
                Visit(conversion.Operand);
                break;
            case PreparedNullCheck check:
                Visit(check.Operand);
                break;
            case PreparedWrappingArithmetic arithmetic:
                Visit(arithmetic.Left);
                Visit(arithmetic.Right);
                break;
            case PreparedStringComparison comparison:
                Visit(comparison.Left);
                Visit(comparison.Right);
                break;
            case PreparedBitCast cast:
                Visit(cast.Operand);
                break;
            case PreparedBuiltinCall call:
                foreach (IrExpression argument in call.Arguments)
                    Visit(argument);
                break;
            case PreparedFunctionCall call:
                foreach (IrExpression argument in call.Arguments)
                    Visit(argument);
                break;
        }
    }
}