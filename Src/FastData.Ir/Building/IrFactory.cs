using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Building;

/// <summary>Short factories for hand-built IR. Import with <c>using static</c>.</summary>
/// <remarks>Factories transfer ownership of supplied argument and statement arrays to the resulting nodes without copying. Callers must not modify them through any alias; immutable nodes may be shared.</remarks>
public static class IrFactory
{
    /// <summary>Reads a variable.</summary>
    public static IrVariableExpression Read(IrVariableId variable) => new IrVariableExpression(variable);

    /// <summary>Reads a data declaration.</summary>
    public static IrDataExpression Read(IrDataId data) => new IrDataExpression(data);

    /// <summary>A Bool literal.</summary>
    public static IrBoolLiteral Bool(bool value) => new IrBoolLiteral(value);

    /// <summary>An I32 literal.</summary>
    public static IrI32Literal I32(int value) => new IrI32Literal(value);

    /// <summary>An I64 literal.</summary>
    public static IrI64Literal I64(long value) => new IrI64Literal(value);

    /// <summary>A U32 literal.</summary>
    public static IrU32Literal U32(uint value) => new IrU32Literal(value);

    /// <summary>A U64 literal.</summary>
    public static IrU64Literal U64(ulong value) => new IrU64Literal(value);

    /// <summary>Creates zero, false, the zero character, or typed null according to the type's default semantics.</summary>
    public static IrLiteral Default(IrType type)
    {
        if (type.AcceptsNull)
            return new IrNullLiteral(type);

        if (type.IsIntegral)
            return IntegerBits(type, 0);

        return type.Kind switch
        {
            IrTypeKind.Bool => new IrBoolLiteral(false),
            IrTypeKind.Char => new IrCharLiteral(type.CharacterKind!.Value, 0),
            IrTypeKind.F32 => new IrF32Literal(0),
            IrTypeKind.F64 => new IrF64Literal(0),
            _ => throw new ArgumentException("The type has no default value.", nameof(type))
        };
    }

    /// <summary>A binary operation.</summary>
    public static IrBinaryExpression Binary(BinaryOperation operation, IrExpression left, IrExpression right) => new IrBinaryExpression(operation, left, right);

    /// <summary><paramref name="left" /> + <paramref name="right" />.</summary>
    public static IrBinaryExpression Add(IrExpression left, IrExpression right) => Binary(BinaryOperation.Add, left, right);

    /// <summary><paramref name="left" /> - <paramref name="right" />.</summary>
    public static IrBinaryExpression Subtract(IrExpression left, IrExpression right) => Binary(BinaryOperation.Subtract, left, right);

    /// <summary><paramref name="left" /> * <paramref name="right" />.</summary>
    public static IrBinaryExpression Multiply(IrExpression left, IrExpression right) => Binary(BinaryOperation.Multiply, left, right);

    /// <summary>Bitwise exclusive disjunction.</summary>
    public static IrBinaryExpression Xor(IrExpression left, IrExpression right) => Binary(BinaryOperation.ExclusiveOr, left, right);

    /// <summary>Unsigned integral division.</summary>
    public static IrBinaryExpression Divide(IrExpression value, IrExpression divisor) => Binary(BinaryOperation.Divide, value, divisor);

    /// <summary>Unsigned integral remainder.</summary>
    public static IrBinaryExpression Mod(IrExpression value, IrExpression divisor) => Binary(BinaryOperation.Modulo, value, divisor);

    /// <summary>Bitwise conjunction.</summary>
    public static IrBinaryExpression And(IrExpression left, IrExpression right) => Binary(BinaryOperation.And, left, right);

    /// <summary>Bitwise disjunction.</summary>
    public static IrBinaryExpression Or(IrExpression left, IrExpression right) => Binary(BinaryOperation.Or, left, right);

    /// <summary>Shifts an unsigned value left by an I32 count.</summary>
    public static IrBinaryExpression ShiftLeft(IrExpression left, IrExpression right) => Binary(BinaryOperation.ShiftLeft, left, right);

    /// <summary>Shifts an unsigned value right by an I32 count.</summary>
    public static IrBinaryExpression ShiftRight(IrExpression left, IrExpression right) => Binary(BinaryOperation.ShiftRight, left, right);

    /// <summary><paramref name="left" /> == <paramref name="right" />.</summary>
    public static IrBinaryExpression Equal(IrExpression left, IrExpression right) => Binary(BinaryOperation.Equal, left, right);

    /// <summary><paramref name="left" /> != <paramref name="right" />.</summary>
    public static IrBinaryExpression NotEqual(IrExpression left, IrExpression right) => Binary(BinaryOperation.NotEqual, left, right);

    /// <summary><paramref name="left" /> &lt; <paramref name="right" />.</summary>
    public static IrBinaryExpression Less(IrExpression left, IrExpression right) => Binary(BinaryOperation.LessThan, left, right);

    /// <summary><paramref name="left" /> &lt;= <paramref name="right" />.</summary>
    public static IrBinaryExpression LessOrEqual(IrExpression left, IrExpression right) => Binary(BinaryOperation.LessThanOrEqual, left, right);

    /// <summary><paramref name="left" /> &gt; <paramref name="right" />.</summary>
    public static IrBinaryExpression Greater(IrExpression left, IrExpression right) => Binary(BinaryOperation.GreaterThan, left, right);

    /// <summary><paramref name="left" /> &gt;= <paramref name="right" />.</summary>
    public static IrBinaryExpression GreaterOrEqual(IrExpression left, IrExpression right) => Binary(BinaryOperation.GreaterThanOrEqual, left, right);

    /// <summary>Boolean negation as equality against false. The operand is evaluated once.</summary>
    public static IrBinaryExpression Not(IrExpression operand) => Equal(operand, Bool(false));

    /// <summary>Integral complement as XOR against a typed all-ones literal. The operand is evaluated once.</summary>
    public static IrBinaryExpression Complement(IrExpression operand) => Xor(operand, IntegerBits(operand.Type, ulong.MaxValue));

    /// <summary>Creates an integral literal from the low bits of a bit pattern at the requested width.</summary>
    public static IrLiteral IntegerBits(IrType type, ulong bits) => unchecked(type.Kind switch
    {
        IrTypeKind.I8 => new IrI8Literal((sbyte)bits),
        IrTypeKind.U8 => new IrU8Literal((byte)bits),
        IrTypeKind.I16 => new IrI16Literal((short)bits),
        IrTypeKind.U16 => new IrU16Literal((ushort)bits),
        IrTypeKind.I32 => I32((int)bits),
        IrTypeKind.U32 => U32((uint)bits),
        IrTypeKind.I64 => I64((long)bits),
        IrTypeKind.U64 => U64(bits),
        _ => throw new ArgumentException("Expected an integral type.", nameof(type))
    });

    /// <summary>Short-circuit conjunction.</summary>
    public static IrLogicalExpression AndAlso(IrExpression left, IrExpression right) => new IrLogicalExpression(LogicalOperation.AndAlso, left, right);

    /// <summary>Short-circuit disjunction.</summary>
    public static IrLogicalExpression OrElse(IrExpression left, IrExpression right) => new IrLogicalExpression(LogicalOperation.OrElse, left, right);

    /// <summary>Reads an array element.</summary>
    public static IrIndexExpression Index(IrExpression collection, IrExpression index) => new IrIndexExpression(collection, index);

    /// <summary>Reads a field of a record value.</summary>
    public static IrFieldExpression Field(IrExpression receiver, IrFieldId field) => new IrFieldExpression(receiver, field);

    /// <summary>Calls a function with argument values.</summary>
    public static IrFunctionCallExpression Call(IrFunctionId function, params IrExpression[] arguments) => new IrFunctionCallExpression(function, arguments);

    /// <summary>A numeric conversion that passes characters through their unit type, or the value itself when the types match.</summary>
    public static IrExpression Convert(IrType target, IrExpression value)
    {
        IrType source = value.Type;
        if (source == target)
            return value;

        if (source.CharacterKind is {} sourceSet)
        {
            source = IrType.UnitOf(sourceSet);
            value = new IrConversionExpression(source, ConversionKind.Numeric, value);
        }

        IrType unitTarget = target.CharacterKind is {} targetSet ? IrType.UnitOf(targetSet) : target;
        if (source != unitTarget)
            value = new IrConversionExpression(unitTarget, ConversionKind.Numeric, value);
        return target == unitTarget ? value : new IrConversionExpression(target, ConversionKind.Numeric, value);
    }

    /// <summary>A block of statements.</summary>
    public static IrBlock Block(params IrStatement[] statements) => new IrBlock(statements);

    /// <summary>Assigns a variable.</summary>
    public static IrAssignmentStatement Assign(IrVariableId variable, IrExpression value) => new IrAssignmentStatement(variable, value);

    /// <summary>Adds one to an I32 variable.</summary>
    public static IrAssignmentStatement Increment(IrVariableId variable) => Assign(variable, Add(Read(variable), I32(1)));

    /// <summary>Returns a value.</summary>
    public static IrReturnStatement Return(IrExpression value) => new IrReturnStatement(value);

    /// <summary>Runs <paramref name="body" /> when <paramref name="condition" /> holds.</summary>
    public static IrIfStatement If(IrExpression condition, params IrStatement[] body) => new IrIfStatement(condition, Block(body));

    /// <summary>Runs one of two blocks.</summary>
    public static IrIfStatement IfElse(IrExpression condition, IrBlock ifTrue, IrBlock ifFalse) => new IrIfStatement(condition, ifTrue, ifFalse);

    /// <summary>Runs <paramref name="body" /> while <paramref name="condition" /> holds.</summary>
    public static IrLoopStatement While(IrExpression condition, params IrStatement[] body) => new IrLoopStatement(condition, Block(body));
}