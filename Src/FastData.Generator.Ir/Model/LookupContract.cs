using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Model;

/// <summary>Designates one slot-index search with optional value storage, enumeration, and exported constants.</summary>
/// <remarks>Stored floating-point keys must be finite. NaN queries miss, and both signed zeros compare equal. These semantics are producer assertions.</remarks>
public sealed class LookupContract
{
    /// <summary>Binds lookup functions, value storage, and exported constants to a program.</summary>
    public LookupContract(IrFunctionId findFunctionId, LookupValues? values = null, LookupEnumeration? enumeration = null, IReadOnlyList<IrDataId>? constants = null)
    {
        FindFunctionId = findFunctionId;
        Values = values;
        Enumeration = enumeration;
        Constants = constants ?? [];
    }

    /// <summary>The function taking one key and returning an I32 slot, or -1 when absent. Membership-only lookups may return any nonnegative hit marker.</summary>
    public IrFunctionId FindFunctionId { get; }

    /// <summary>The optional value storage.</summary>
    public LookupValues? Values { get; }

    /// <summary>The optional key and value cursor functions.</summary>
    public LookupEnumeration? Enumeration { get; }

    /// <summary>Data IDs selected for export as public constants. Their declarations supply the names, types, and values; emission follows program data order.</summary>
    public IReadOnlyList<IrDataId> Constants { get; }
}