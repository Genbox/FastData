using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Lowered;
using Genbox.FastData.Generators;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Lowering;

internal sealed class ExpressionTranslator(FastDataProgramBuilder builder, Func<string, IrType, IrVariableId> declareLocal, Dictionary<ParameterExpression, IrVariableId> variables, ExpressionTranslator.SharedData data)
{
    public IrBlock TranslateLambda(LambdaExpression lambda)
    {
        LoweredProgram program = ExpressionProgramLowerer.LowerLambda(lambda, nameof(ExpressionTranslator));
        return Translate(program, program.Body);
    }

    /// <summary>Translates one statement-level assignment, such as an early-exit allocation.</summary>
    public IrStatement Assignment(BinaryExpression assignment)
    {
        if (assignment.Left is not ParameterExpression target)
            throw new NotSupportedException("Assignment targets must be variables.");

        IrVariableId variable = Variable(target);
        IrExpression value = Value(assignment.Right);
        if (assignment.NodeType == ExpressionType.Assign)
            return Assign(variable, value);

        return Assign(variable, Operator(assignment.NodeType, null, Read(variable), value));
    }

    public IrExpression Value(Expression expression) => expression switch
    {
        ParameterExpression parameter => Read(Variable(parameter)),
        ConstantExpression constant => IrClrConverter.FromValue(constant.Value, IrClrConverter.FromClrType(constant.Type), builder.Program.Text.Encoding),
        DefaultExpression defaultValue => Default(IrClrConverter.FromClrType(defaultValue.Type)),
        UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert => Convert(IrClrConverter.FromClrType(convert.Type), Value(convert.Operand)),
        UnaryExpression { NodeType: ExpressionType.Not } not => not.Type == typeof(bool)
            ? Not(Value(not.Operand))
            : Complement(Value(not.Operand)),
        BinaryExpression { NodeType: ExpressionType.AndAlso } binary => AndAlso(Value(binary.Left), Value(binary.Right)),
        BinaryExpression { NodeType: ExpressionType.OrElse } binary => OrElse(Value(binary.Left), Value(binary.Right)),
        BinaryExpression { NodeType: ExpressionType.ArrayIndex } index => Index(Value(index.Left), Value(index.Right)),
        BinaryExpression binary => Operator(binary.NodeType, binary.Left, Value(binary.Left), Value(binary.Right)),
        MemberExpression { Expression: ConstantExpression closure } member => Read(data.Get(member, closure)),
        MethodCallExpression call => GeneratorCall(call),
        _ => throw new NotSupportedException($"Expression '{expression.NodeType}' is not supported by the IR lowering.")
    };

    private IrBlock Translate(LoweredProgram program, LoweredBlock block)
    {
        List<IrStatement> statements = new List<IrStatement>(block.Statements.Count);
        Append(program, block, statements);
        return new IrBlock(statements);
    }

    private void Append(LoweredProgram program, LoweredBlock block, List<IrStatement> statements)
    {
        foreach (ParameterExpression local in block.Locals)
        {
            IrVariableId variable = Variable(local);
            if (program.RequiresDefaultInitialization(local))
                statements.Add(Assign(variable, Default(IrClrConverter.FromClrType(local.Type))));
        }

        foreach (LoweredStatement statement in block.Statements)
            Append(program, statement, statements);
    }

    private void Append(LoweredProgram program, LoweredStatement statement, List<IrStatement> statements)
    {
        switch (statement)
        {
            case LoweredBlock nested:
                Append(program, nested, statements);
                break;
            case LoweredAssignmentStatement assignment:
                statements.Add(Assignment((BinaryExpression)assignment.Operation.Source));
                break;
            case LoweredIfStatement conditional:
                statements.Add(new IrIfStatement(Value(conditional.Test.Source), Translate(program, conditional.IfTrue), conditional.IfFalse == null ? null : Translate(program, conditional.IfFalse)));
                break;
            case LoweredWhileStatement loop:
                statements.Add(new IrLoopStatement(Value(loop.Test.Source), Translate(program, loop.Body)));
                break;
            case LoweredReturnStatement { Value: {} value }:
                statements.Add(Return(Value(value.Source)));
                break;
            case LoweredBreakStatement:
                statements.Add(new IrBreakStatement());
                break;
            default:
                throw new NotSupportedException($"Statement '{statement.GetType().Name}' is not supported by the IR lowering.");
        }
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity distinguishes declarations.")]
    private IrVariableId Variable(ParameterExpression parameter)
    {
        if (!variables.TryGetValue(parameter, out IrVariableId id))
        {
            id = declareLocal(parameter.Name ?? "local", IrClrConverter.FromClrType(parameter.Type));
            variables.Add(parameter, id);
        }

        return id;
    }

    private static IrExpression Operator(ExpressionType operation, Expression? leftSource, IrExpression left, IrExpression right)
    {
        IrType leftType = left.Type;
        BinaryOperation binary = operation switch
        {
            ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.AddAssign or ExpressionType.AddAssignChecked => BinaryOperation.Add,
            ExpressionType.Subtract or ExpressionType.SubtractChecked or ExpressionType.SubtractAssign or ExpressionType.SubtractAssignChecked => BinaryOperation.Subtract,
            ExpressionType.Multiply or ExpressionType.MultiplyChecked or ExpressionType.MultiplyAssign or ExpressionType.MultiplyAssignChecked => BinaryOperation.Multiply,
            ExpressionType.Divide => BinaryOperation.Divide,
            ExpressionType.Modulo => BinaryOperation.Modulo,
            ExpressionType.And or ExpressionType.AndAssign => BinaryOperation.And,
            ExpressionType.Or or ExpressionType.OrAssign => BinaryOperation.Or,
            ExpressionType.ExclusiveOr or ExpressionType.ExclusiveOrAssign => BinaryOperation.ExclusiveOr,
            ExpressionType.LeftShift or ExpressionType.LeftShiftAssign => BinaryOperation.ShiftLeft,
            ExpressionType.RightShift or ExpressionType.RightShiftAssign => BinaryOperation.ShiftRight,
            ExpressionType.Equal => BinaryOperation.Equal,
            ExpressionType.NotEqual => BinaryOperation.NotEqual,
            ExpressionType.LessThan => BinaryOperation.LessThan,
            ExpressionType.LessThanOrEqual => BinaryOperation.LessThanOrEqual,
            ExpressionType.GreaterThan => BinaryOperation.GreaterThan,
            ExpressionType.GreaterThanOrEqual => BinaryOperation.GreaterThanOrEqual,
            _ => throw new NotSupportedException($"Operator '{operation}' is not supported by the IR lowering.")
        };

        if (binary == BinaryOperation.And && leftType == IrType.Bool)
            return AndAlso(left, right);

        if (binary == BinaryOperation.Or && leftType == IrType.Bool)
            return OrElse(left, right);

        if (binary == BinaryOperation.ExclusiveOr && leftType == IrType.Bool)
            return NotEqual(left, right);

        if (binary is BinaryOperation.ShiftLeft or BinaryOperation.ShiftRight)
        {
            IrType resultType = leftType;

            // CLR shifts promote narrow integers and mask counts to five or six bits.
            if (leftType.BitWidth < 32)
            {
                left = Convert(IrType.I32, left);
                leftType = IrType.I32;
            }

            int mask = leftType.BitWidth - 1;
            right = right is IrI32Literal count ? I32(count.Value & mask) : And(right, I32(mask));

            IrExpression shifted;

            if (!leftType.IsSignedIntegral)
                shifted = Binary(binary, left, right);
            else if (binary == BinaryOperation.ShiftLeft)
            {
                IrType unsigned = IrType.UnsignedOf(leftType);
                shifted = Convert(leftType, Binary(binary, Convert(unsigned, left), right));
            }
            else
                shifted = leftSource != null && IsNonNegative(leftSource) ? ShiftRightNonNegative(left, right) : ShiftRightArithmetic(left, right);

            return Convert(resultType, shifted);
        }

        if (leftType.IsSignedIntegral && binary is BinaryOperation.Divide or BinaryOperation.Modulo)
            throw new NotSupportedException($"Signed '{binary}' has no IR equivalent; the lowering needs an unsigned operand.");

        // C# signed arithmetic wraps, while IR signed overflow is a precondition violation. The unsigned bits wrap identically.
        if (leftType.IsSignedIntegral && binary is BinaryOperation.Add or BinaryOperation.Subtract or BinaryOperation.Multiply)
        {
            IrType unsigned = IrType.UnsignedOf(leftType);
            return Convert(leftType, Binary(binary, Convert(unsigned, left), Convert(unsigned, right)));
        }

        return Binary(binary, left, right);
    }

    // A nonnegative signed value has the same bits as its unsigned counterpart, so a logical shift is exact.
    private static IrExpression ShiftRightNonNegative(IrExpression value, IrExpression count)
    {
        IrType type = value.Type;
        IrType unsigned = IrType.UnsignedOf(type);
        return Convert(type, Binary(BinaryOperation.ShiftRight, Convert(unsigned, value), count));
    }

    // C# shifts signed values arithmetically: with s = 0 or -1 holding the sign, x >> n == ((x ^ s) >>> n) ^ s.
    private static IrBinaryExpression ShiftRightArithmetic(IrExpression value, IrExpression count)
    {
        IrType type = value.Type;
        IrType unsigned = IrType.UnsignedOf(type);
        IrExpression signBit = Convert(type, Binary(BinaryOperation.ShiftRight, Convert(unsigned, value), I32(type.BitWidth - 1)));
        IrExpression sign = Subtract(Default(type), signBit);
        IrExpression magnitude = ShiftRightNonNegative(Xor(value, sign), count);
        return Xor(magnitude, sign);
    }

    /// <summary>Recognizes values that are nonnegative by construction: constants, masks, and zero-extended unsigned values.</summary>
    private static bool IsNonNegative(Expression expression) => expression switch
    {
        ConstantExpression { Value: {} value } => NonNegativeInteger(value) != null,
        UnaryExpression { NodeType: ExpressionType.Convert } convert => (Numeric(convert.Operand.Type).IsUnsignedIntegral && Numeric(convert.Operand.Type).BitWidth < Numeric(convert.Type).BitWidth)
                                                                        || IsNonNegativeMask(convert.Operand, convert.Type),
        BinaryExpression { NodeType: ExpressionType.And } and => IsNonNegative(and.Left) || IsNonNegative(and.Right),
        _ => false
    };

    // (x & mask) with a mask below the target's sign bit stays nonnegative after conversion.
    private static bool IsNonNegativeMask(Expression expression, Type target) =>
        expression is BinaryExpression { NodeType: ExpressionType.And } and
        && ((and.Right is ConstantExpression { Value: {} mask } && Fits(mask, target)) || (and.Left is ConstantExpression { Value: {} other } && Fits(other, target)));

    private static bool Fits(object mask, Type target) => NonNegativeInteger(mask) is {} value && value < 1UL << (Numeric(target).BitWidth - 1);

    // The value of a nonnegative integral constant, or null for negative and non-integral constants.
    private static ulong? NonNegativeInteger(object value) => value switch
    {
        sbyte x and >= 0 => (ulong)x,
        short x and >= 0 => (ulong)x,
        int x and >= 0 => (ulong)x,
        long x and >= 0 => (ulong)x,
        byte x => x,
        ushort x => x,
        uint x => x,
        ulong x => x,
        char x => x,
        _ => null
    };

    // Characters take part as their unit type.
    private static IrType Numeric(Type type)
    {
        IrType ir = IrClrConverter.FromClrType(type);
        return ir.CharacterKind is {} charSet ? IrType.UnitOf(charSet) : ir;
    }

    private IrExpression GeneratorCall(MethodCallExpression call)
    {
        if (call.Method.DeclaringType != typeof(GeneratorFunctions))
            throw new NotSupportedException($"Method '{call.Method.DeclaringType}.{call.Method.Name}' is not supported by the IR lowering.");

        LookupText text = builder.Text;
        IrExpression Argument(int index) => Value(call.Arguments[index]);

        switch (call.Method.Name)
        {
            case nameof(GeneratorFunctions.Length):
                return text.Length(Argument(0));
            case nameof(GeneratorFunctions.IsAsciiOnly):
                return text.IsAscii(Argument(0));
            case nameof(GeneratorFunctions.UnitAt):
            case nameof(GeneratorFunctions.UnitAtAsciiLower):
            {
                IrFunctionId? fold = call.Method.Name == nameof(GeneratorFunctions.UnitAtAsciiLower) ? data.AsciiFold : null;
                IrExpression unit = text.UnitAt(Argument(0), Offset(call.Arguments[0], call.Arguments[1]), fold);
                return Convert(IrType.U32, unit);
            }
            case nameof(GeneratorFunctions.EqualsAt):
            case nameof(GeneratorFunctions.EqualsAtAsciiLower):
            {
                IrFunctionId? fold = call.Method.Name == nameof(GeneratorFunctions.EqualsAtAsciiLower) ? data.AsciiFold : null;

                // EqualsAt uses any negative offset to select the entire suffix.
                if (call.Arguments[1] is ConstantExpression { Value: int offset } && offset < 0)
                    return text.EndsWith(Argument(0), Argument(2), fold);
                return text.EqualsAt(Argument(0), Offset(call.Arguments[0], call.Arguments[1]), Argument(2), fold);
            }
            case nameof(GeneratorFunctions.ReadU8):
                return builder.Builtins.Call(IrBuiltinOperation.ReadU8, Argument(0), Argument(1));
            case nameof(GeneratorFunctions.ReadU16):
                return builder.Builtins.Call(IrBuiltinOperation.ReadU16LittleEndian, Argument(0), Argument(1));
            case nameof(GeneratorFunctions.ReadU32):
                return builder.Builtins.Call(IrBuiltinOperation.ReadU32LittleEndian, Argument(0), Argument(1));
            case nameof(GeneratorFunctions.ReadU64):
                return builder.Builtins.Call(IrBuiltinOperation.ReadU64LittleEndian, Argument(0), Argument(1));
            default:
                throw new NotSupportedException($"Generator function '{call.Method.Name}' is not supported by the IR lowering.");
        }
    }

    /// <summary>Generator functions treat a negative constant offset as relative to the end of the text.</summary>
    private IrExpression Offset(Expression text, Expression offset)
    {
        if (offset is ConstantExpression { Value: int constant })
        {
            return constant >= 0
                ? I32(constant)
                : Add(builder.Text.Length(Value(text)), I32(constant));
        }

        throw new NotSupportedException("Generator function offsets must be constants.");
    }

    /// <summary>Data and helpers shared by every translator of one program.</summary>
    internal sealed class SharedData(IrProgramBuilder program, IrFunctionId? keyFold)
    {
        private readonly ConditionalWeakTable<object, Dictionary<MemberInfo, IrDataId>> _data = new ConditionalWeakTable<object, Dictionary<MemberInfo, IrDataId>>();
        private IrFunctionId? _asciiFold = keyFold;

        /// <summary>The ASCII fold, reusing the key fold when the lookup ignores case.</summary>
        public IrFunctionId AsciiFold => _asciiFold ??= LookupText.CreateAsciiFold(program);

        public IrDataId Get(MemberExpression member, ConstantExpression closure)
        {
            // Captured objects use reference identity, and each member has its own stored value.
            Dictionary<MemberInfo, IrDataId> members = _data.GetOrCreateValue(closure.Value!);

            if (!members.TryGetValue(member.Member, out IrDataId id))
            {
                IrLiteral value = IrClrConverter.FromValue(ReadMember(member, closure), IrClrConverter.FromClrType(member.Type), program.Text.Encoding);
                id = program.AddData(member.Member.Name, value);
                members.Add(member.Member, id);
            }

            return id;
        }

        private static object ReadMember(MemberExpression member, ConstantExpression closure) => member.Member switch
        {
            FieldInfo field => field.GetValue(closure.Value)!,
            PropertyInfo property => property.GetValue(closure.Value)!,
            _ => throw new NotSupportedException($"Captured member '{member.Member.Name}' is not a field or property.")
        };
    }
}