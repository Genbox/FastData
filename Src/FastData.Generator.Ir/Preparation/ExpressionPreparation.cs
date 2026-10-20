using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Extensions;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Preparation;

/// <summary>Resolves runtime operations and makes target integer promotion explicit in emission expressions.</summary>
internal sealed class ExpressionPreparation(IrProgram program, IrFunction function, IrBackendCapabilities capabilities, IReadOnlyList<string> names)
{
    private readonly Dictionary<IrExpression, (IrExpression Expression, IrType Type)> _prepared = new Dictionary<IrExpression, (IrExpression, IrType)>();

    public IrExpression Prepare(IrExpression expression)
    {
        Stack<(IrExpression Expression, bool Visited)> pending = new Stack<(IrExpression, bool)>();
        pending.Push((expression, false));

        while (pending.Count > 0)
        {
            (IrExpression current, bool visited) = pending.Pop();
            if (_prepared.ContainsKey(current))
                continue;

            if (!visited)
            {
                pending.Push((current, true));

                switch (current)
                {
                    case IrIndexExpression index:
                        pending.Push((index.Index, false));
                        pending.Push((index.Collection, false));
                        break;
                    case IrFieldExpression field:
                        pending.Push((field.Receiver, false));
                        break;
                    case IrBinaryExpression binary:
                        pending.Push((binary.Right, false));
                        pending.Push((binary.Left, false));
                        break;
                    case IrLogicalExpression logical:
                        pending.Push((logical.Right, false));
                        pending.Push((logical.Left, false));
                        break;
                    case IrConversionExpression conversion:
                        pending.Push((conversion.Operand, false));
                        break;
                    case IrFunctionCallExpression call:
                        for (int i = call.Arguments.Count - 1; i >= 0; i--)
                            pending.Push((call.Arguments[i], false));
                        break;
                }

                continue;
            }

            // Children already carry their prepared form and result type, so each node is visited once.
            _prepared.Add(current, current switch
            {
                IrLiteral literal => (literal, literal.Type),
                IrVariableExpression variable => (variable, function.GetVariable(variable.VariableId).Type),
                IrDataExpression data => (data, program.Resolve(data.DataId).Type),
                IrIndexExpression index => (PrepareIndex(index), TypeOf(index.Collection).ElementType!),
                IrFieldExpression field => (PrepareField(field), program.Resolve(field.FieldId).Type),
                IrLogicalExpression logical => (PrepareLogical(logical), IrType.Bool),
                IrBinaryExpression binary => (PrepareBinary(binary), binary.Operation.IsComparison() ? IrType.Bool : TypeOf(binary.Left)),
                IrConversionExpression conversion => (PrepareConversion(conversion), conversion.Type),
                IrFunctionCallExpression call => (PrepareCall(call), program.ResolveCallable(call.FunctionId).Signature.ReturnType),
                _ => throw new NotSupportedException($"Expression '{current.GetType().Name}' is not supported by target preparation.")
            });
        }

        return _prepared[expression].Expression;
    }

    private IrExpression Prepared(IrExpression expression) => _prepared[expression].Expression;

    private IrIndexExpression PrepareIndex(IrIndexExpression index)
    {
        IrExpression collection = Prepared(index.Collection);
        IrExpression offset = Prepared(index.Index);
        return ReferenceEquals(collection, index.Collection) && ReferenceEquals(offset, index.Index) ? index : new IrIndexExpression(collection, offset);
    }

    private IrFieldExpression PrepareField(IrFieldExpression field)
    {
        IrExpression receiver = Prepared(field.Receiver);
        return ReferenceEquals(receiver, field.Receiver) ? field : new IrFieldExpression(receiver, field.FieldId);
    }

    private IrLogicalExpression PrepareLogical(IrLogicalExpression logical)
    {
        IrExpression left = Prepared(logical.Left);
        IrExpression right = Prepared(logical.Right);
        return ReferenceEquals(left, logical.Left) && ReferenceEquals(right, logical.Right) ? logical : new IrLogicalExpression(logical.Operation, left, right);
    }

    private IrExpression PrepareConversion(IrConversionExpression conversion)
    {
        IrExpression operand = Prepared(conversion.Operand);
        if (conversion.Conversion == ConversionKind.BitCast)
            return new PreparedBitCast(TypeOf(conversion.Operand), conversion.Type, operand);
        return ReferenceEquals(operand, conversion.Operand) ? conversion : new IrConversionExpression(conversion.Type, conversion.Conversion, operand);
    }

    private IrExpression PrepareBinary(IrBinaryExpression binary)
    {
        IrType type = TypeOf(binary.Left);
        IrExpression left = Prepared(binary.Left);
        IrExpression right = Prepared(binary.Right);

        if (binary.Operation is BinaryOperation.Equal or BinaryOperation.NotEqual)
        {
            if (binary.Left is IrNullLiteral)
                return new PreparedNullCheck(right, binary.Operation == BinaryOperation.Equal);

            if (binary.Right is IrNullLiteral)
                return new PreparedNullCheck(left, binary.Operation == BinaryOperation.Equal);
        }

        if (type == IrType.String)
            return new PreparedStringComparison(binary.Operation, left, right);

        bool wrappingOperation = type.IsUnsignedIntegral && binary.Operation is BinaryOperation.Add or BinaryOperation.Subtract or BinaryOperation.Multiply;
        bool promotedProductCanOverflow = binary.Operation == BinaryOperation.Multiply && type.BitWidth < capabilities.IntegerPromotionWidth
                                                                                       && type.BitWidth * 2 >= capabilities.IntegerPromotionWidth;
        if (wrappingOperation && (capabilities.ExplicitWrappingArithmetic || promotedProductCanOverflow))
            return new PreparedWrappingArithmetic(type, binary.Operation, left, right);

        IrBinaryExpression result = ReferenceEquals(left, binary.Left) && ReferenceEquals(right, binary.Right) ? binary : new IrBinaryExpression(binary.Operation, left, right);

        if (type.IsIntegral && type.BitWidth < capabilities.IntegerPromotionWidth && !binary.Operation.IsComparison())
        {
            // The target evaluates narrow arithmetic at a wider width; restore the IR result before its parent consumes it.
            return new IrConversionExpression(type, ConversionKind.Numeric, result);
        }

        return result;
    }

    private IrExpression PrepareCall(IrFunctionCallExpression call)
    {
        IrExpression[]? replacements = null;

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            IrExpression argument = Prepared(call.Arguments[i]);

            if (!ReferenceEquals(argument, call.Arguments[i]))
                replacements ??= call.Arguments.ToArray();

            if (replacements != null)
                replacements[i] = argument;
        }

        IReadOnlyList<IrExpression> arguments = replacements ?? call.Arguments;

        if (program.ResolveCallable(call.FunctionId) is IrExternalFunction external)
        {
            if (!IrBuiltins.TryGetOperation(external.Symbol, out IrBuiltinOperation operation))
                throw new NotSupportedException($"Unknown external function '{external.Symbol}'.");

            return new PreparedBuiltinCall(operation, arguments, call.Type);
        }

        return new PreparedFunctionCall(names[call.FunctionId.Value], arguments, call.Type);
    }

    private IrType TypeOf(IrExpression expression) => _prepared[expression].Type;
}