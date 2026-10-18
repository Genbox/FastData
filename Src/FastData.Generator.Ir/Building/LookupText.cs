using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Building;

/// <summary>Encoding-aware lookup text operations and reusable compound helpers. Generation-time encoding and literals are on <see cref="IrText" />.</summary>
/// <remarks>Offsets and distances use the program's code units. String operands must be non-null. All inputs must satisfy the program's encoding contract.</remarks>
public sealed class LookupText
{
    private readonly IrBuiltinBuilder _builtins;
    private readonly Dictionary<IrFunctionId, IrFunctionId> _endsWithFolded = new Dictionary<IrFunctionId, IrFunctionId>();
    private readonly Dictionary<IrFunctionId, IrFunctionId> _equalsAtFolded = new Dictionary<IrFunctionId, IrFunctionId>();
    private readonly IrProgramBuilder _program;
    private IrFunctionId? _endsWith;
    private IrFunctionId? _unitFromEnd;

    internal LookupText(IrProgramBuilder program, IrBuiltinBuilder builtins)
    {
        _program = program;
        _builtins = builtins;
    }

    /// <summary>Counts encoded code units.</summary>
    public IrFunctionCallExpression Length(IrExpression text) => _builtins.Call(IrBuiltinOperation.TextLength, text);

    /// <summary>Reads a U8 or U16 code unit at a nonnegative in-bounds offset.</summary>
    public IrFunctionCallExpression UnitAt(IrExpression text, IrExpression offset, IrFunctionId? foldFunctionId = null)
    {
        IrFunctionCallExpression call = _builtins.Call(IrBuiltinOperation.TextUnitAt, text, offset);
        if (foldFunctionId is {} fold)
            return Call(fold, call);

        return call;
    }

    /// <summary>Compares encoded fragment units at a nonnegative offset where the fragment fits.</summary>
    public IrFunctionCallExpression EqualsAt(IrExpression text, IrExpression offset, IrExpression fragment, IrFunctionId? foldFunctionId = null)
    {
        if (foldFunctionId is not {} fold)
            return _builtins.Call(IrBuiltinOperation.TextEqualsAt, text, offset, fragment);

        if (!_equalsAtFolded.TryGetValue(fold, out IrFunctionId helper))
        {
            helper = AddEqualsAt(fold);
            _equalsAtFolded.Add(fold, helper);
        }

        return Call(helper, text, offset, fragment);
    }

    /// <summary>Tests whether every encoded unit is below 128.</summary>
    public IrFunctionCallExpression IsAscii(IrExpression text) => _builtins.Call(IrBuiltinOperation.TextIsAscii, text);

    /// <summary>Reads a unit counted from the end. Distance 1 means the last unit; distance must be between 1 and the text length.</summary>
    public IrFunctionCallExpression UnitFromEnd(IrExpression text, IrExpression distance, IrFunctionId? foldFunctionId = null)
    {
        IrFunctionCallExpression call = Call(_unitFromEnd ??= AddUnitFromEnd(), text, distance);
        return foldFunctionId is {} fold ? Call(fold, call) : call;
    }

    /// <summary>Tests an encoded-unit suffix. An empty fragment matches; a fragment longer than the input does not.</summary>
    public IrFunctionCallExpression EndsWith(IrExpression text, IrExpression fragment, IrFunctionId? foldFunctionId = null)
    {
        if (foldFunctionId is not {} fold)
            return Call(_endsWith ??= AddEndsWith(null), text, fragment);

        if (!_endsWithFolded.TryGetValue(fold, out IrFunctionId helper))
        {
            helper = AddEndsWith(fold);
            _endsWithFolded.Add(fold, helper);
        }

        return Call(helper, text, fragment);
    }

    /// <summary>Declares the predefined pure ASCII A-Z to a-z fold.</summary>
    public static IrFunctionId CreateAsciiFold(IrProgramBuilder program)
    {
        IrType unitType = program.Text.CodeUnitType;
        IrFunctionBuilder function = program.Function("ToAsciiLower", unitType);
        IrVariableId unit = function.Parameter("unit", unitType);
        IrExpression isUppercase = AndAlso(GreaterOrEqual(Read(unit), program.Text.Unit('A')), LessOrEqual(Read(unit), program.Text.Unit('Z')));
        IrExpression lowercase = Add(Read(unit), program.Text.Unit('a' - 'A'));
        function.Define(Block(If(isUppercase, Return(lowercase)), Return(Read(unit))));
        return function.Id;
    }

    /// <summary>Declares a unit-by-unit region comparison. The ordinal form supplies the TextEqualsAt fallback.</summary>
    internal IrFunctionId AddEqualsAt(IrFunctionId? fold)
    {
        IrFunctionBuilder function = _program.Function(fold == null ? nameof(IrBuiltinOperation.TextEqualsAt) : "EqualsAtFolded", IrType.Bool);
        IrVariableId text = function.Parameter("text", IrType.String);
        IrVariableId offset = function.Parameter("offset", IrType.I32);
        IrVariableId fragment = function.Parameter("fragment", IrType.String);
        IrVariableId length = function.Local("length", IrType.I32);
        IrVariableId index = function.Local("index", IrType.I32);
        IrExpression textIndex = Add(Read(offset), Read(index));
        IrExpression left = UnitAt(Read(text), textIndex, fold);
        IrExpression right = UnitAt(Read(fragment), Read(index), fold);
        function.Define(Block(Assign(length, Length(Read(fragment))),
            Assign(index, I32(0)),
            While(Less(Read(index), Read(length)),
                If(NotEqual(left, right), Return(Bool(false))),
                Increment(index)),
            Return(Bool(true))));
        return function.Id;
    }

    private IrFunctionId AddUnitFromEnd()
    {
        IrFunctionBuilder function = _program.Function("UnitFromEnd", _program.Text.CodeUnitType);
        IrVariableId text = function.Parameter("text", IrType.String);
        IrVariableId distance = function.Parameter("distance", IrType.I32);
        IrExpression offset = Subtract(Length(Read(text)), Read(distance));
        function.Define(Block(Return(UnitAt(Read(text), offset))));
        return function.Id;
    }

    private IrFunctionId AddEndsWith(IrFunctionId? fold)
    {
        IrFunctionBuilder function = _program.Function(fold == null ? "EndsWith" : "EndsWithFolded", IrType.Bool);
        IrVariableId text = function.Parameter("text", IrType.String);
        IrVariableId fragment = function.Parameter("fragment", IrType.String);
        IrVariableId textLength = function.Local("textLength", IrType.I32);
        IrVariableId fragmentLength = function.Local("fragmentLength", IrType.I32);
        IrExpression offset = Subtract(Read(textLength), Read(fragmentLength));
        IrExpression matches = EqualsAt(Read(text), offset, Read(fragment), fold);

        function.Define(Block(Assign(textLength, Length(Read(text))),
            Assign(fragmentLength, Length(Read(fragment))),
            If(Greater(Read(fragmentLength), Read(textLength)), Return(Bool(false))),
            Return(matches)));

        return function.Id;
    }
}