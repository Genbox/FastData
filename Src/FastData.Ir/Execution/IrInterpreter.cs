using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Execution;

/// <summary>Executes a validated program directly, as the reference semantics that generated code must match.</summary>
/// <remarks>
/// Operators use the executable semantics in IrOperations. Required operands and arguments evaluate once.
/// This interpreter chooses left-to-right evaluation; backends may choose another order.
/// Null/bounds and integral zero-divisor failures provide diagnostics for producer precondition violations.
/// Missing-handler failures are interpreter diagnostics.
/// Immutable aggregate literals are reused; arrays and records support only null comparisons.
/// </remarks>
public sealed class IrInterpreter
{
    private readonly IrExternalHandler? _externals;
    private readonly IrProgram _program;

    /// <summary>Creates an interpreter; <paramref name="externals" /> executes calls to external functions.</summary>
    public IrInterpreter(IrProgram program, IrExternalHandler? externals = null)
    {
        _program = program;
        _externals = externals;
    }

    /// <summary>Calls a function with one value per parameter and returns its value.</summary>
    public IrLiteral Invoke(IrFunctionId id, params IrLiteral[] arguments)
    {
        IrFunction function = _program.Resolve(id);

        if (arguments.Length != function.Parameters.Count)
            throw new ArgumentException($"'{function.Name}' takes {function.Parameters.Count} arguments.", nameof(arguments));

        for (int i = 0; i < arguments.Length; i++)
        {
            IrVariable parameter = function.Parameters[i];
            IrLiteral argument = arguments[i];

            if (argument is IrNullLiteral or IrCharLiteral or IrStringLiteral or IrArrayLiteralBase or IrRecordLiteral)
            {
                try
                {
                    IrProgramValidation.ValidateLiteral(_program, argument);
                }
                catch (IrValidationException exception)
                {
                    throw new ArgumentException($"Argument {i} is incompatible with this program: {exception.Message}", nameof(arguments));
                }
            }

            if (parameter.Type != argument.Type)
                throw new ArgumentException($"Argument {i} has type {argument.Type}, but '{parameter.Name}' has type {parameter.Type}.", nameof(arguments));
        }

        return Call(function, arguments);
    }

    private IrLiteral Call(IrFunction function, IrLiteral[] arguments)
    {
        Frame frame = new Frame(function);
        Array.Copy(arguments, frame.Variables, arguments.Length);
        Execute(function.Body, frame);
        return frame.ReturnValue!;
    }

    private Completion Execute(IrBlock block, Frame frame)
    {
        for (int i = 0; i < block.Statements.Count; i++)
        {
            Completion completion = Execute(block.Statements[i], frame);
            if (completion != Completion.Normal)
                return completion;
        }

        return Completion.Normal;
    }

    private Completion Execute(IrStatement statement, Frame frame)
    {
        switch (statement)
        {
            case IrAssignmentStatement assignment:
                frame.Variables[assignment.VariableId.Value] = Evaluate(assignment.Value, frame);
                return Completion.Normal;
            case IrIfStatement branch:
                if (IsTrue(branch.Condition, frame))
                    return Execute(branch.IfTrue, frame);

                return branch.IfFalse == null ? Completion.Normal : Execute(branch.IfFalse, frame);
            case IrLoopStatement loop:
                while (IsTrue(loop.Condition, frame))
                {
                    Completion completion = Execute(loop.Body, frame);
                    if (completion == Completion.Break)
                        break;
                    if (completion == Completion.Return)
                        return completion;
                }

                return Completion.Normal;
            case IrBreakStatement:
                return Completion.Break;
            case IrReturnStatement returned:
                frame.ReturnValue = Evaluate(returned.Value, frame);
                return Completion.Return;
            default:
                throw new NotSupportedException($"Unsupported IR statement '{statement.GetType().Name}'.");
        }
    }

    private IrLiteral Evaluate(IrExpression expression, Frame frame)
    {
        switch (expression)
        {
            case IrLiteral literal:
                return literal;
            case IrDataExpression data:
                return _program.Resolve(data.DataId).Value;
            case IrVariableExpression variable:
                return frame.Variables[variable.VariableId.Value]!;
            case IrFieldExpression field:
            {
                IrLiteral instance = Evaluate(field.Receiver, frame);
                return instance is not IrRecordLiteral record ? throw new IrExecutionException("A field was read from a null record.") : record.Fields[field.FieldId.Value];
            }
            case IrIndexExpression index:
                return Element(Evaluate(index.Collection, frame), Evaluate(index.Index, frame));
            case IrBinaryExpression binary:
            {
                IrLiteral left = Evaluate(binary.Left, frame);
                IrLiteral right = Evaluate(binary.Right, frame);

                return IrOperations.Binary(binary.Operation, left, right, _program.TextEncoding);
            }
            case IrLogicalExpression logical:
            {
                bool left = IsTrue(logical.Left, frame);
                return new IrBoolLiteral(logical.Operation == LogicalOperation.AndAlso ? left && IsTrue(logical.Right, frame) : left || IsTrue(logical.Right, frame));
            }
            case IrConversionExpression conversion:
                return IrOperations.Convert(conversion.Conversion, conversion.TargetType, Evaluate(conversion.Operand, frame));
            case IrFunctionCallExpression call:
                return EvaluateCall(call, frame);
            default:
                throw new NotSupportedException($"Unsupported IR expression '{expression.GetType().Name}'.");
        }
    }

    private IrLiteral EvaluateCall(IrFunctionCallExpression call, Frame frame)
    {
        IrLiteral[] arguments = new IrLiteral[call.Arguments.Count];
        for (int i = 0; i < arguments.Length; i++)
            arguments[i] = Evaluate(call.Arguments[i], frame);

        IrCallable callable = _program.ResolveCallable(call.FunctionId);
        if (callable is IrFunction function)
            return Call(function, arguments);

        IrExternalFunction external = (IrExternalFunction)callable;
        IrExternalHandler handler = _externals ?? throw new IrExecutionException($"No handler executes external function '{external.Symbol}'.");
        return handler(external, arguments);
    }

    private static IrLiteral Element(IrLiteral collection, IrLiteral position)
    {
        int index = ((IrI32Literal)position).Value;

        return collection switch
        {
            IrArrayLiteralBase array when index >= 0 && index < array.Count => array.ElementAt(index),
            IrNullLiteral => throw new IrExecutionException("An element was read from a null array."),
            _ => throw new IrExecutionException($"Index {index} is outside the array.")
        };
    }

    private bool IsTrue(IrExpression condition, Frame frame) => ((IrBoolLiteral)Evaluate(condition, frame)).Value;

    private enum Completion
    {
        Unknown = 0,
        Normal,
        Break,
        Return
    }

    private sealed class Frame(IrFunction function)
    {
        public IrLiteral?[] Variables { get; } = new IrLiteral?[function.VariableCount];
        public IrLiteral? ReturnValue { get; set; }
    }
}