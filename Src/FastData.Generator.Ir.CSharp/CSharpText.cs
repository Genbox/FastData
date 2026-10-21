using System.Globalization;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.CSharp;

/// <summary>Formats C# text types, literals, comparisons, and runtime operations.</summary>
public sealed class CSharpText(GeneratorEncoding encoding)
{
    private static readonly IrBuiltinOperation[] _readOperations =
    [
        IrBuiltinOperation.ReadU8,
        IrBuiltinOperation.ReadU16LittleEndian,
        IrBuiltinOperation.ReadU32LittleEndian,
        IrBuiltinOperation.ReadU64LittleEndian
    ];

    /// <summary>Whether text uses UTF-16 code units.</summary>
    public bool IsUtf16 { get; } = encoding == GeneratorEncoding.Utf16CodeUnits;

    /// <summary>The C# text type.</summary>
    public string Type { get; } = encoding == GeneratorEncoding.Utf16CodeUnits ? "string" : "byte[]";

    private static (string Name, string ReadType, string ResultType) ReadHelper(IrBuiltinOperation operation) => operation switch
    {
        IrBuiltinOperation.ReadU8 => ("ReadU8", "byte", "uint"),
        IrBuiltinOperation.ReadU16LittleEndian => ("ReadU16", "ushort", "uint"),
        IrBuiltinOperation.ReadU32LittleEndian => ("ReadU32", "uint", "uint"),
        IrBuiltinOperation.ReadU64LittleEndian => ("ReadU64", "ulong", "ulong"),
        _ => throw new NotSupportedException($"Unsupported byte-read builtin '{operation}'.")
    };

    /// <summary>Formats content equality, including null values.</summary>
    public string Equal(string left, string right)
    {
        if (IsUtf16)
            return $"{left} == {right}";

        return $"(ReferenceEquals({left}, {right}) || ({left} is not null && {right} is not null && ({left}).AsSpan().SequenceEqual(({right}).AsSpan())))";
    }

    /// <summary>Formats content equality or ordering in the selected encoding.</summary>
    public string StringComparison(BinaryOperation operation, string left, string right) => operation switch
    {
        BinaryOperation.Equal => Equal(left, right),
        BinaryOperation.NotEqual => IsUtf16 ? $"{left} != {right}" : "!" + Equal(left, right),
        _ => $"{(IsUtf16 ? "string.CompareOrdinal" : "CompareText")}({left}, {right}) {CStyleOperators.Symbol(operation)} 0"
    };

    /// <summary>Formats a string literal in the selected encoding.</summary>
    internal string Literal(ReadOnlySpan<char> value)
    {
        if (IsUtf16)
            return Quote(value, '"', static (text, unit) => text.Append(@"\u").Append(unit.ToString("X4", CultureInfo.InvariantCulture)));

        return $"new byte[] {{ {string.Join(", ", EncodeBytes(value, encoding).Select(InvariantNumber))} }}";
    }

    /// <summary>Formats a UTF-16 character literal.</summary>
    internal static string Character(uint value) => Quote(((char)value).ToString(), '\'', static (text, unit) => text.Append(@"\u").Append(unit.ToString("X4", CultureInfo.InvariantCulture)));

    internal static string Call(IrBuiltinOperation operation, string[] arguments) => operation switch
    {
        IrBuiltinOperation.ReadU8 or IrBuiltinOperation.ReadU16LittleEndian or IrBuiltinOperation.ReadU32LittleEndian or IrBuiltinOperation.ReadU64LittleEndian => $"{ReadHelper(operation).Name}({arguments[0]}, {arguments[1]})",
        IrBuiltinOperation.TextUnitAt => $"{arguments[0]}[{arguments[1]}]",
        IrBuiltinOperation.TextLength => $"{arguments[0]}.Length",
        IrBuiltinOperation.TextEqualsAt => $"({arguments[0]}).AsSpan({arguments[1]}).StartsWith(({arguments[2]}).AsSpan())",
        IrBuiltinOperation.TextIsAscii => $"System.Text.Ascii.IsValid({arguments[0]})",
        _ => throw new NotSupportedException($"Builtin '{operation}' has no C# implementation.")
    };

    internal IEnumerable<string> Helpers(IrBuiltinOperation required)
    {
        if (!IsUtf16)
            yield return "private static int CompareText(byte[]? left, byte[]? right) => ReferenceEquals(left, right) ? 0 : left is null ? -1 : right is null ? 1 : left.AsSpan().SequenceCompareTo(right.AsSpan());";

        foreach (IrBuiltinOperation operation in _readOperations)
        {
            if (required.HasFlag(operation))
                yield return ReadHelperSource(ReadHelper(operation));
        }
    }

    // UTF-16 byte offsets refer to little-endian code units; byte arrays already use byte offsets.
    // Generated code targets little-endian hosts.
    private string ReadHelperSource((string Name, string ReadType, string ResultType) helper)
    {
        string firstByte = IsUtf16
            ? "Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(text.AsSpan()))"
            : "MemoryMarshal.GetArrayDataReference(text)";

        return $"""
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static {helper.ResultType} {helper.Name}({Type} text, int offset) => Unsafe.ReadUnaligned<{helper.ReadType}>(ref Unsafe.Add(ref {firstByte}, offset));
                """;
    }
}