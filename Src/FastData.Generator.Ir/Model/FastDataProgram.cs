using Genbox.FastData.Generator.Ir.Validation;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Model;

/// <summary>A validated lookup: a generic program, its lookup bindings, and exported constants.</summary>
/// <remarks>Created by <see cref="FastDataValidator" /> after checking structural contracts and builtin signatures. Producers remain responsible for search results and stored-key semantics.</remarks>
public sealed class FastDataProgram
{
    internal FastDataProgram(IrProgram program, LookupContract contract, IrType keyType, IrType? valueType)
    {
        Program = program;
        Contract = contract;
        KeyType = keyType;
        ValueType = valueType;
    }

    /// <summary>The validated generic program.</summary>
    public IrProgram Program { get; }

    /// <summary>The lookup bindings, value storage, enumeration, and exported constants.</summary>
    public LookupContract Contract { get; }

    /// <summary>The key type, taken from Find's parameter.</summary>
    public IrType KeyType { get; }

    /// <summary>The stored array element or record field type, or <see langword="null" /> for membership-only lookups.</summary>
    public IrType? ValueType { get; }
}