using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Building;

/// <summary>Key comparison and hashing that lower to generic operations and program-local helpers.</summary>
public sealed class LookupKeys
{
    private readonly IrBuiltinBuilder _builtins;
    private readonly IrType _keyType;
    private readonly IrProgramBuilder _program;
    private IrFunctionId? _compareFunctionId;
    private IrFunctionId? _coordinateId;
    private IrFunctionId? _equalFunctionId;
    private IrFunctionId? _numericHashId;

    internal LookupKeys(IrProgramBuilder program, IrBuiltinBuilder builtins, IrType keyType, IrFunctionId? foldFunctionId)
    {
        _program = program;
        _builtins = builtins;
        _keyType = keyType;
        FoldFunctionId = foldFunctionId;
    }

    /// <summary>The code-unit fold, or <see langword="null" /> for ordinal comparison.</summary>
    public IrFunctionId? FoldFunctionId { get; }

    /// <summary>Compares keys using the optional code-unit fold, or core equality for ordinal comparison. Folded String operands must be non-null.</summary>
    public IrExpression Equal(IrExpression left, IrExpression right)
    {
        if (FoldFunctionId != null)
            return Call(_equalFunctionId ??= AddFoldedEquality(), left, right);

        return IrFactory.Equal(left, right);
    }

    /// <summary>Orders keys using the optional fold, returning a negative, zero, or positive I32. Characters order by unit value. Floating-point operands must be ordered IEEE values, and String operands must be non-null.</summary>
    public IrExpression Compare(IrExpression left, IrExpression right) => Call(_compareFunctionId ??= FoldFunctionId != null ? AddFoldedComparison() : AddComparison(), left, right);

    private IrFunctionId AddComparison()
    {
        if (!_keyType.IsNumeric && _keyType.Kind != IrTypeKind.Char && _keyType != IrType.String)
            throw new InvalidOperationException("Ordering requires numeric, character, or string keys.");

        IrFunctionBuilder compare = _program.Function("CompareKeys", IrType.I32);
        IrVariableId left = compare.Parameter("left", _keyType);
        IrVariableId right = compare.Parameter("right", _keyType);
        compare.Define(Block(If(Less(Read(left), Read(right)), Return(I32(-1))),
            If(Greater(Read(left), Read(right)), Return(I32(1))),
            Return(I32(0))));
        return compare.Id;
    }

    private IrFunctionId AddFoldedEquality()
    {
        IrFunctionBuilder function = _program.Function("EqualKeys", IrType.Bool);
        IrVariableId left = function.Parameter("left", IrType.String);
        IrVariableId right = function.Parameter("right", IrType.String);
        IrExpression sameLength = IrFactory.Equal(_builtins.Text.Length(Read(left)), _builtins.Text.Length(Read(right)));
        IrExpression matches = _builtins.Text.EqualsAt(Read(left), I32(0), Read(right), FoldFunctionId);

        // The length check also guarantees that the fragment fits before EqualsAt reads its units.
        function.Define(Block(Return(AndAlso(sameLength, matches))));
        return function.Id;
    }

    private IrFunctionId AddFoldedComparison()
    {
        IrFunctionBuilder function = _program.Function("CompareKeys", IrType.I32);
        IrVariableId left = function.Parameter("left", IrType.String);
        IrVariableId right = function.Parameter("right", IrType.String);
        IrVariableId leftLength = function.Local("leftLength", IrType.I32);
        IrVariableId rightLength = function.Local("rightLength", IrType.I32);
        IrVariableId index = function.Local("index", IrType.I32);
        IrVariableId a = function.Local("a", _program.Text.CodeUnitType);
        IrVariableId b = function.Local("b", _program.Text.CodeUnitType);

        // Query and stored keys are non-null.
        function.Define(Block(Assign(leftLength, _builtins.Text.Length(Read(left))),
            Assign(rightLength, _builtins.Text.Length(Read(right))),
            Assign(index, I32(0)),
            While(AndAlso(Less(Read(index), Read(leftLength)), Less(Read(index), Read(rightLength))),
                Assign(a, _builtins.Text.UnitAt(Read(left), Read(index), FoldFunctionId)),
                Assign(b, _builtins.Text.UnitAt(Read(right), Read(index), FoldFunctionId)),
                If(Less(Read(a), Read(b)), Return(I32(-1))),
                If(Greater(Read(a), Read(b)), Return(I32(1))),
                Increment(index)),
            Return(Subtract(Read(leftLength), Read(rightLength)))));
        return function.Id;
    }

    /// <summary>Declares the numeric hash helper once. Characters hash as their unit value. Both signed zeros hash identically.</summary>
    public IrFunctionId AddNumericHashHelper() => _numericHashId ??= AddNumericHash();

    private IrFunctionId AddNumericHash()
    {
        if (!_keyType.IsIntegral && !_keyType.IsFloatingPoint && _keyType.Kind != IrTypeKind.Char)
            throw new InvalidOperationException("The built-in numeric hash supports integral, character, and binary floating-point keys.");

        IrFunctionBuilder function = _program.Function("HashKey", IrType.U64);
        IrVariableId key = function.Parameter("hashKey", _keyType);

        // Convert characters through their unit type to hash their code-unit value.
        IrBlock body = _keyType.IsFloatingPoint ? FloatingPointHashBody(key) : Block(Return(Convert(IrType.U64, Read(key))));
        function.Define(body);
        return function.Id;
    }

    private IrBlock FloatingPointHashBody(IrVariableId key)
    {
        bool isF32 = _keyType == IrType.F32;
        IrType bitsType = isF32 ? IrType.U32 : IrType.U64;
        IrExpression bits = new IrConversionExpression(bitsType, ConversionKind.BitCast, Read(key));
        IrExpression unchangedBits = Convert(IrType.U64, bits);

        // IEEE equality recognizes both signed zeros; every other representation hashes unchanged.
        return Block(If(IrFactory.Equal(Read(key), Default(_keyType)), Return(U64(0))), Return(unchangedBits));
    }

    /// <summary>Maps numeric keys to U64 prediction coordinates. Floating-point coordinates preserve order and normalize signed zero; integral coordinates use unsigned conversion.</summary>
    public IrExpression Coordinate(IrExpression value)
    {
        if (_keyType.IsFloatingPoint)
            return Call(_coordinateId ??= AddCoordinate(), value);

        return Convert(IrType.U64, value);
    }

    private IrFunctionId AddCoordinate()
    {
        IrType bitsType = _keyType == IrType.F32 ? IrType.U32 : IrType.U64;
        ulong sign = _keyType == IrType.F32 ? 0x80000000 : 0x8000000000000000;
        IrLiteral Bits(ulong value) => IntegerBits(bitsType, value);
        IrFunctionBuilder function = _program.Function("Coordinate", IrType.U64);
        IrVariableId key = function.Parameter("key", _keyType);
        IrVariableId bits = function.Local("bits", bitsType);
        function.Define(Block(If(IrFactory.Equal(Read(key), Default(_keyType)), Return(U64(sign))),
            Assign(bits, new IrConversionExpression(bitsType, ConversionKind.BitCast, Read(key))),
            If(NotEqual(And(Read(bits), Bits(sign)), Bits(0)), Return(Convert(IrType.U64, Complement(Read(bits))))),
            Return(Convert(IrType.U64, Xor(Read(bits), Bits(sign))))));
        return function.Id;
    }
}