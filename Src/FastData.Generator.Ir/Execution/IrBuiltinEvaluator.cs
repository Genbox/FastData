using System.Runtime.CompilerServices;
using System.Text;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Generator.Ir.Execution;

/// <summary>The reference implementation of every lookup builtin, for generation-time evaluation and for <see cref="IrInterpreter" />.</summary>
/// <remarks>
/// Results depend only on argument values and the fixed encoding; internal encoding caches affect cost only.
/// Detected producer-precondition violations
/// throw <see cref="IrExecutionException" /> as reference-execution diagnostics.
/// Text payloads are validated before entering the IR. ASCII checks inspect characters directly.
/// </remarks>
public sealed class IrBuiltinEvaluator
{
    private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false, true);
    private static readonly ConditionalWeakTable<string, byte[]>.CreateValueCallback _encodeText = _utf8.GetBytes;
    private readonly ConditionalWeakTable<string, byte[]> _encodedText = new ConditionalWeakTable<string, byte[]>();
    private readonly IrText _text;

    /// <summary>Creates an evaluator for programs that use <paramref name="encoding" />.</summary>
    public IrBuiltinEvaluator(IrTextEncoding encoding)
    {
        _text = new IrText(encoding);
    }

    /// <summary>The text encoding that offsets and code units refer to.</summary>
    public IrTextEncoding Encoding => _text.Encoding;

    /// <summary>Creates an <see cref="IrInterpreter" /> external handler that evaluates catalog builtins in <paramref name="program" />'s text encoding.</summary>
    /// <remarks>Binding to the program fixes the encoding used for offsets and lengths.</remarks>
    /// <exception cref="IrValidationException">A catalog declaration in <paramref name="program" /> has an incompatible signature.</exception>
    public static IrExternalHandler CreateHandler(IrProgram program)
    {
        IrBuiltins.Validate(program);
        return new IrBuiltinEvaluator(program.TextEncoding).Handle;
    }

    // Unknown external symbols and trapped calls produce IrExecutionException.
    private IrLiteral Handle(IrExternalFunction function, IReadOnlyList<IrLiteral> arguments)
    {
        if (!IrBuiltins.TryGetOperation(function.Symbol, out IrBuiltinOperation operation))
            throw new IrExecutionException($"External function '{function.Symbol}' is not a lookup builtin.");

        // Dispatch relies on the operand types established by program validation.
        return Execute(operation, arguments);
    }

    private IrLiteral Execute(IrBuiltinOperation operation, IReadOnlyList<IrLiteral> arguments) => operation switch
    {
        IrBuiltinOperation.TextUnitAt => TextUnitAt(arguments[0], arguments[1]),
        IrBuiltinOperation.TextLength => new IrI32Literal(TextLength(Text(arguments[0]))),
        IrBuiltinOperation.TextEqualsAt => TextEqualsAt(arguments[0], arguments[1], arguments[2]),
        IrBuiltinOperation.TextIsAscii => new IrBoolLiteral(Ascii.IsValid(Text(arguments[0]).AsSpan())),
        IrBuiltinOperation.ReadU8 => new IrU32Literal((uint)Read(arguments[0], arguments[1], 1)),
        IrBuiltinOperation.ReadU16LittleEndian => new IrU32Literal((uint)Read(arguments[0], arguments[1], 2)),
        IrBuiltinOperation.ReadU32LittleEndian => new IrU32Literal((uint)Read(arguments[0], arguments[1], 4)),
        IrBuiltinOperation.ReadU64LittleEndian => new IrU64Literal(Read(arguments[0], arguments[1], 8)),
        IrBuiltinOperation.MultiplyHigh => new IrU64Literal(Math.BigMul(((IrU64Literal)arguments[0]).Value, ((IrU64Literal)arguments[1]).Value, out _)),
        IrBuiltinOperation.PopCount => new IrI32Literal((int)ulong.PopCount(((IrU64Literal)arguments[0]).Value)),
        IrBuiltinOperation.TrailingZeroCount => new IrI32Literal((int)ulong.TrailingZeroCount(((IrU64Literal)arguments[0]).Value)),
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported builtin operation.")
    };

    private IrLiteral TextUnitAt(IrLiteral text, IrLiteral offset)
    {
        TextUnits units = Units(text);
        int index = ((IrI32Literal)offset).Value;
        if (index < 0 || index >= units.Length)
            throw new IrExecutionException($"Text offset {index} is outside text of length {units.Length}.");

        return _text.Unit((uint)units[index]);
    }

    private IrBoolLiteral TextEqualsAt(IrLiteral text, IrLiteral offset, IrLiteral fragment)
    {
        TextUnits units = Units(text);
        TextUnits expected = Units(fragment);
        int start = ((IrI32Literal)offset).Value;
        if (start < 0 || (long)start + expected.Length > units.Length)
            throw new IrExecutionException($"A fragment of length {expected.Length} at offset {start} does not fit text of length {units.Length}.");

        return new IrBoolLiteral(units.EqualsAt(start, expected));
    }

    private int TextLength(string value)
    {
        if (Encoding == IrTextEncoding.Utf16)
            return value.Length;

        if (_encodedText.TryGetValue(value, out byte[]? encoded))
            return encoded.Length;

        return _text.GetLength(value);
    }

    private TextUnits Units(IrLiteral text)
    {
        string value = Text(text);

        // UTF-16 is already represented by the string. Weak keys let repeated unit reads share encoding
        // while allowing released interpreter inputs to be collected.
        return Encoding == IrTextEncoding.Utf16 ? new TextUnits(value) : new TextUnits(_encodedText.GetValue(value, _encodeText));
    }

    private static string Text(IrLiteral literal) => literal is IrStringLiteral text ? text.Value : throw new IrExecutionException("Builtin text arguments must be non-null.");

    private ulong Read(IrLiteral text, IrLiteral offset, int size)
    {
        TextUnits units = Units(text);
        int unitSize = Encoding == IrTextEncoding.Utf8 ? 1 : 2;
        long length = (long)units.Length * unitSize;
        int start = ((IrI32Literal)offset).Value;
        if (start < 0 || (long)start + size > length)
            throw new IrExecutionException($"A {size}-byte read at offset {start} does not fit {length} bytes.");

        ulong value = 0;

        for (int i = size - 1; i >= 0; i--)
        {
            // Units expose their bytes low byte first, independent of host endianness.
            long byteIndex = (long)start + i;
            int unit = (units[(int)(byteIndex / unitSize)] >> (int)((byteIndex % unitSize) * 8)) & 0xff;
            value = (value << 8) | (uint)unit;
        }

        return value;
    }

    private readonly struct TextUnits
    {
        private readonly string? _text;
        private readonly byte[]? _units;

        public TextUnits(string text)
        {
            _text = text;
        }

        public TextUnits(byte[] units)
        {
            _units = units;
        }

        public bool EqualsAt(int offset, TextUnits expected) => _text != null
            ? _text.AsSpan(offset, expected.Length).SequenceEqual(expected._text.AsSpan())
            : _units.AsSpan(offset, expected.Length).SequenceEqual(expected._units.AsSpan());

        public int Length => _text?.Length ?? _units!.Length;
        public int this[int index] => _text != null ? _text[index] : _units![index];
    }
}