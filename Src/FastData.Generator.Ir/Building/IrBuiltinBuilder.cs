using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Building;

/// <summary>Declares lookup builtins once per program: natively supported operations as external functions, the rest as program-local fallback functions.</summary>
public sealed class IrBuiltinBuilder
{
    private const ulong Low32Mask = 0xFFFFFFFF;

    private readonly Dictionary<IrBuiltinOperation, IrFunctionId> _declarations = new Dictionary<IrBuiltinOperation, IrFunctionId>();
    private readonly IrProgramBuilder _program;

    /// <summary>Creates a declaration cache for a single program builder.</summary>
    /// <param name="program">The program that receives the declarations.</param>
    /// <param name="supported">The operations the target generator implements natively. It must include <see cref="IrBuiltins.Required" />.</param>
    /// <exception cref="ArgumentException"><paramref name="supported" /> omits a required primitive or contains undefined bits.</exception>
    public IrBuiltinBuilder(IrProgramBuilder program, IrBuiltinOperation supported = IrBuiltins.All)
    {
        IrBuiltins.ValidateSupport(supported);

        _program = program;
        Supported = supported;
        Text = new LookupText(program, this);
    }

    /// <summary>The operations declared as external functions; every other operation is declared as a fallback function.</summary>
    public IrBuiltinOperation Supported { get; }

    /// <summary>Encoding-aware text operations using this declaration cache.</summary>
    public LookupText Text { get; }

    /// <summary>Declares a builtin once per program, as an external function when supported and as a fallback function otherwise.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operation" /> must identify exactly one declared operation.</exception>
    public IrFunctionId Declare(IrBuiltinOperation operation)
    {
        if (_declarations.TryGetValue(operation, out IrFunctionId id))
            return id;

        IrFunctionSignature signature = IrBuiltins.GetSignature(operation, _program.Text.Encoding);

        if (Supported.HasFlag(operation))
            id = _program.ExternalFunction(IrBuiltins.GetSymbol(operation), signature);
        else
            id = DeclareFallback(operation);

        _declarations.Add(operation, id);
        return id;
    }

    /// <summary>Builds a normal call to a builtin, evaluating arguments exactly once in an unspecified order.</summary>
    public IrFunctionCallExpression Call(IrBuiltinOperation operation, params IrExpression[] arguments) => IrFactory.Call(Declare(operation), arguments);

    private IrFunctionId DeclareFallback(IrBuiltinOperation operation) => operation switch
    {
        IrBuiltinOperation.TextEqualsAt => Text.AddEqualsAt(null),
        IrBuiltinOperation.TextIsAscii => AddIsAscii(),
        IrBuiltinOperation.ReadU8 => AddRead(operation, 1),
        IrBuiltinOperation.ReadU16LittleEndian => AddRead(operation, 2),
        IrBuiltinOperation.ReadU32LittleEndian => AddRead(operation, 4),
        IrBuiltinOperation.ReadU64LittleEndian => AddRead(operation, 8),
        IrBuiltinOperation.MultiplyHigh => AddMultiplyHigh(),
        IrBuiltinOperation.PopCount => AddPopCount(),
        IrBuiltinOperation.TrailingZeroCount => AddTrailingZeroCount(),
        _ => throw new InvalidOperationException($"The builtin '{operation}' has no IR fallback and must be supported natively.")
    };

    private IrFunctionId AddIsAscii()
    {
        IrFunctionBuilder function = _program.Function(nameof(IrBuiltinOperation.TextIsAscii), IrType.Bool);
        IrVariableId text = function.Parameter("text", IrType.String);
        IrVariableId length = function.Local("length", IrType.I32);
        IrVariableId index = function.Local("index", IrType.I32);
        IrLiteral limit = _program.Text.Unit(0x80);
        IrExpression unit = Text.UnitAt(Read(text), Read(index));

        function.Define(Block(Assign(length, Text.Length(Read(text))),
            Assign(index, I32(0)),
            While(Less(Read(index), Read(length)),
                If(GreaterOrEqual(unit, limit), Return(Bool(false))),
                Increment(index)),
            Return(Bool(true))));
        return function.Id;
    }

    private IrExpression ReadByte(IrExpression text, IrExpression offset)
    {
        if (_program.Text.Encoding == IrTextEncoding.Utf8)
            return Convert(IrType.U32, Text.UnitAt(text, Convert(IrType.I32, offset)));

        // Unsigned byte offsets cover both bytes of every UTF-16 unit, including reads crossing Int32.MaxValue.
        IrExpression unitIndex = Convert(IrType.I32, ShiftRight(offset, I32(1)));
        IrExpression shift = Convert(IrType.I32, Multiply(And(offset, U32(1)), U32(8)));
        IrExpression unit = Convert(IrType.U32, Text.UnitAt(text, unitIndex));
        return And(ShiftRight(unit, shift), U32(0xFF));
    }

    private IrFunctionId AddRead(IrBuiltinOperation operation, int size)
    {
        IrType resultType = size == 8 ? IrType.U64 : IrType.U32;
        IrFunctionBuilder function = _program.Function(operation.ToString(), resultType);
        IrVariableId text = function.Parameter("text", IrType.String);
        IrVariableId offset = function.Parameter("byteOffset", IrType.I32);
        IrExpression byteOffset = Convert(IrType.U32, Read(offset));
        IrExpression value = Convert(resultType, ReadByte(Read(text), byteOffset));

        // Unrolled little-endian assembly: byte k is shifted left by 8 * k.
        for (int k = 1; k < size; k++)
        {
            IrExpression part = Convert(resultType, ReadByte(Read(text), Add(byteOffset, U32((uint)k))));

            value = Or(value, ShiftLeft(part, I32(8 * k)));
        }

        function.Define(Block(Return(value)));
        return function.Id;
    }

    private IrFunctionId AddMultiplyHigh()
    {
        IrFunctionBuilder function = _program.Function(nameof(IrBuiltinOperation.MultiplyHigh), IrType.U64);
        IrVariableId a = function.Parameter("a", IrType.U64);
        IrVariableId b = function.Parameter("b", IrType.U64);
        IrVariableId p00 = function.Local("p00", IrType.U64);
        IrVariableId p01 = function.Local("p01", IrType.U64);
        IrVariableId p10 = function.Local("p10", IrType.U64);
        IrVariableId p11 = function.Local("p11", IrType.U64);
        IrVariableId carry = function.Local("carry", IrType.U64);

        // 32-bit limbs; every partial product and sum fits in U64.
        IrExpression Low(IrExpression value) => And(value, U64(Low32Mask));
        IrExpression High(IrExpression value) => ShiftRight(value, I32(32));

        IrExpression middleSum = Add(Add(High(Read(p00)), Low(Read(p01))), Low(Read(p10)));
        IrExpression highWord = Add(Add(Add(Read(p11), High(Read(p01))), High(Read(p10))), Read(carry));

        function.Define(Block(Assign(p00, Multiply(Low(Read(a)), Low(Read(b)))),
            Assign(p01, Multiply(Low(Read(a)), High(Read(b)))),
            Assign(p10, Multiply(High(Read(a)), Low(Read(b)))),
            Assign(p11, Multiply(High(Read(a)), High(Read(b)))),
            Assign(carry, High(middleSum)),
            Return(highWord)));
        return function.Id;
    }

    private IrFunctionId AddPopCount()
    {
        IrFunctionBuilder function = _program.Function(nameof(IrBuiltinOperation.PopCount), IrType.I32);
        IrVariableId value = function.Parameter("value", IrType.U64);
        IrVariableId count = function.Local("count", IrType.I32);

        // Each iteration clears the lowest set bit.
        function.Define(Block(Assign(count, I32(0)),
            While(NotEqual(Read(value), U64(0)),
                Assign(value, And(Read(value), Subtract(Read(value), U64(1)))),
                Increment(count)),
            Return(Read(count))));
        return function.Id;
    }

    private IrFunctionId AddTrailingZeroCount()
    {
        IrFunctionBuilder function = _program.Function(nameof(IrBuiltinOperation.TrailingZeroCount), IrType.I32);
        IrVariableId value = function.Parameter("value", IrType.U64);
        IrVariableId count = function.Local("count", IrType.I32);

        function.Define(Block(If(Equal(Read(value), U64(0)), Return(I32(64))),
            Assign(count, I32(0)),
            While(Equal(And(Read(value), U64(1)), U64(0)),
                Assign(value, ShiftRight(Read(value), I32(1))),
                Increment(count)),
            Return(Read(count))));
        return function.Id;
    }
}