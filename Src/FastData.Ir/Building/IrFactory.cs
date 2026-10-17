using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Building;

/// <summary>Short factories for hand-built IR. Import with <c>using static</c>.</summary>
public static class IrFactory
{
    /// <summary>Reads a variable.</summary>
    public static IrVariableExpression Read(IrVariableId id) => new IrVariableExpression(id);

    /// <summary>Reads a data declaration.</summary>
    public static IrDataExpression Read(IrDataId id) => new IrDataExpression(id);

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

        return type.Kind switch
        {
            IrTypeKind.Bool => new IrBoolLiteral(false),
            IrTypeKind.Char => new IrCharLiteral(type.CharacterKind!.Value, 0),
            IrTypeKind.I8 => new IrI8Literal(0),
            IrTypeKind.U8 => new IrU8Literal(0),
            IrTypeKind.I16 => new IrI16Literal(0),
            IrTypeKind.U16 => new IrU16Literal(0),
            IrTypeKind.I32 => new IrI32Literal(0),
            IrTypeKind.U32 => new IrU32Literal(0),
            IrTypeKind.I64 => new IrI64Literal(0),
            IrTypeKind.U64 => new IrU64Literal(0),
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
    public static IrBinaryExpression Complement(IrType type, IrExpression operand)
    {
        IrLiteral mask = type.Kind switch
        {
            IrTypeKind.I8 => new IrI8Literal(-1),
            IrTypeKind.U8 => new IrU8Literal(byte.MaxValue),
            IrTypeKind.I16 => new IrI16Literal(-1),
            IrTypeKind.U16 => new IrU16Literal(ushort.MaxValue),
            IrTypeKind.I32 => I32(-1),
            IrTypeKind.U32 => U32(uint.MaxValue),
            IrTypeKind.I64 => I64(-1),
            IrTypeKind.U64 => U64(ulong.MaxValue),
            _ => throw new ArgumentException("Complement requires an integral type.", nameof(type))
        };
        return Binary(BinaryOperation.ExclusiveOr, operand, mask);
    }

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

    /// <summary>A numeric conversion.</summary>
    public static IrConversionExpression Convert(IrType target, IrExpression value) => new IrConversionExpression(target, ConversionKind.Numeric, value);

    /// <summary>A numeric conversion from <paramref name="source" /> that passes characters through their unit type, or <paramref name="value" /> itself when the types match.</summary>
    public static IrExpression Convert(IrType target, IrExpression value, IrType source)
    {
        if (source == target)
            return value;

        if (source.CharacterKind is {} sourceSet)
        {
            source = IrType.UnitOf(sourceSet);
            value = Convert(source, value);
        }

        IrType unitTarget = target.CharacterKind is {} targetSet ? IrType.UnitOf(targetSet) : target;
        if (source != unitTarget)
            value = Convert(unitTarget, value);
        return target == unitTarget ? value : Convert(target, value);
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