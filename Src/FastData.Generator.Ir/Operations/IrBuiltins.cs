using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Generator.Ir.Operations;

/// <summary>Defines builtin symbols and signatures and validates their declarations.</summary>
/// <remarks>All catalog operations satisfy external purity for valid arguments under the program's fixed text encoding.</remarks>
public static class IrBuiltins
{
    /// <summary>The text primitives every generator must implement natively.</summary>
    public const IrBuiltinOperation Required = IrBuiltinOperation.TextUnitAt | IrBuiltinOperation.TextLength;

    /// <summary>Byte reads over encoded text.</summary>
    public const IrBuiltinOperation ByteReads = IrBuiltinOperation.ReadU8 | IrBuiltinOperation.ReadU16LittleEndian | IrBuiltinOperation.ReadU32LittleEndian | IrBuiltinOperation.ReadU64LittleEndian;

    /// <summary>Every builtin operation.</summary>
    public const IrBuiltinOperation All = IrBuiltinOperation.TextUnitAt | IrBuiltinOperation.TextLength | IrBuiltinOperation.TextEqualsAt | IrBuiltinOperation.TextIsAscii |
                                          ByteReads |
                                          IrBuiltinOperation.MultiplyHigh | IrBuiltinOperation.PopCount | IrBuiltinOperation.TrailingZeroCount;

    private static readonly Dictionary<IrBuiltinOperation, (string Symbol, IrFunctionSignature? Signature)> _definitions = new Dictionary<IrBuiltinOperation, (string, IrFunctionSignature?)>(11)
    {
        [IrBuiltinOperation.TextUnitAt] = ("fastdata.lookup.text.unitAt", null),
        [IrBuiltinOperation.TextLength] = ("fastdata.lookup.text.length", new IrFunctionSignature(IrType.I32, [IrType.String])),
        [IrBuiltinOperation.TextEqualsAt] = ("fastdata.lookup.text.equalsAt", new IrFunctionSignature(IrType.Bool, [IrType.String, IrType.I32, IrType.String])),
        [IrBuiltinOperation.TextIsAscii] = ("fastdata.lookup.text.isAscii", new IrFunctionSignature(IrType.Bool, [IrType.String])),
        [IrBuiltinOperation.ReadU8] = ("fastdata.lookup.read.u8", new IrFunctionSignature(IrType.U32, [IrType.String, IrType.I32])),
        [IrBuiltinOperation.ReadU16LittleEndian] = ("fastdata.lookup.read.u16LittleEndian", new IrFunctionSignature(IrType.U32, [IrType.String, IrType.I32])),
        [IrBuiltinOperation.ReadU32LittleEndian] = ("fastdata.lookup.read.u32LittleEndian", new IrFunctionSignature(IrType.U32, [IrType.String, IrType.I32])),
        [IrBuiltinOperation.ReadU64LittleEndian] = ("fastdata.lookup.read.u64LittleEndian", new IrFunctionSignature(IrType.U64, [IrType.String, IrType.I32])),
        [IrBuiltinOperation.MultiplyHigh] = ("fastdata.lookup.multiplyHigh", new IrFunctionSignature(IrType.U64, [IrType.U64, IrType.U64])),
        [IrBuiltinOperation.PopCount] = ("fastdata.lookup.popCount", new IrFunctionSignature(IrType.I32, [IrType.U64])),
        [IrBuiltinOperation.TrailingZeroCount] = ("fastdata.lookup.trailingZeroCount", new IrFunctionSignature(IrType.I32, [IrType.U64]))
    };
    private static readonly Dictionary<string, IrBuiltinOperation> _operations = _definitions.ToDictionary(static definition => definition.Value.Symbol, static definition => definition.Key, StringComparer.Ordinal);
    private static readonly IrFunctionSignature _utf8UnitAt = new IrFunctionSignature(IrType.U8, [IrType.String, IrType.I32]);
    private static readonly IrFunctionSignature _utf16UnitAt = new IrFunctionSignature(IrType.U16, [IrType.String, IrType.I32]);

    internal static void ValidateSupport(IrBuiltinOperation supported)
    {
        if (!All.HasFlag(supported))
            throw new ArgumentException("The supported builtins contain undefined operations.", nameof(supported));

        if (!supported.HasFlag(Required))
            throw new ArgumentException("The supported builtins must include TextUnitAt and TextLength.", nameof(supported));
    }

    /// <summary>The stable external linkage symbol.</summary>
    public static string GetSymbol(IrBuiltinOperation operation) => GetDefinition(operation).Symbol;

    private static (string Symbol, IrFunctionSignature? Signature) GetDefinition(IrBuiltinOperation operation)
    {
        if (_definitions.TryGetValue(operation, out (string Symbol, IrFunctionSignature? Signature) definition))
            return definition;

        throw new ArgumentOutOfRangeException(nameof(operation), operation, "Expected exactly one builtin operation.");
    }

    /// <summary>Recognizes exact catalog symbols.</summary>
    public static bool TryGetOperation(string symbol, out IrBuiltinOperation operation) => _operations.TryGetValue(symbol, out operation);

    /// <summary>Resolves a builtin signature. Every signature is fixed, except that TextUnitAt returns the encoding's code-unit type.</summary>
    public static IrFunctionSignature GetSignature(IrBuiltinOperation operation, IrTextEncoding encoding) => GetDefinition(operation).Signature ?? encoding switch
    {
        IrTextEncoding.Utf8 => _utf8UnitAt,
        IrTextEncoding.Utf16 => _utf16UnitAt,
        _ => throw new ArgumentOutOfRangeException(nameof(encoding))
    };

    /// <summary>Validates every catalog declaration in a generic program, including unused declarations.</summary>
    /// <exception cref="IrValidationException">A catalog declaration has an incompatible signature.</exception>
    public static void Validate(IrProgram program)
    {
        IrDiagnosticPath root = IrDiagnosticPath.Root();

        for (int i = 0; i < program.Callables.Count; i++)
        {
            if (program.Callables[i] is not IrExternalFunction function)
                continue;

            // Symbols outside the catalog are ordinary opaque externals.
            if (TryGetOperation(function.Symbol, out IrBuiltinOperation operation))
                CheckSignature(function.Signature, operation, program.TextEncoding, root.Named("Callables", function.Symbol, i));
        }
    }

    private static void CheckSignature(IrFunctionSignature declared, IrBuiltinOperation operation, IrTextEncoding encoding, IrDiagnosticPath path)
    {
        IrFunctionSignature expected = GetSignature(operation, encoding);

        IrDiagnosticPath signature = path + ".Signature";
        Require(expected.Parameters.Count == declared.Parameters.Count, "Builtin parameter count does not match its catalog signature.", signature + ".Parameters", IrDiagnosticCode.TypeMismatch);
        Require(expected.ReturnType == declared.ReturnType, "Builtin return type does not match its catalog signature.", signature + ".ReturnType", IrDiagnosticCode.TypeMismatch);

        // Parameter types must match the catalog signature exactly.
        for (int i = 0; i < expected.Parameters.Count; i++)
            Require(declared.Parameters[i] == expected.Parameters[i], "Builtin parameter type does not match its catalog signature.", signature.Index("Parameters", i), IrDiagnosticCode.TypeMismatch);
    }
}