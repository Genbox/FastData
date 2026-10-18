using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Model;

/// <summary>Immutable value storage indexed by the slot returned from the search.</summary>
/// <param name="DataId">An array of values or records containing the values.</param>
/// <param name="FieldId">The value field of each record element, or <see langword="null" /> to use the whole array element as the value.</param>
public sealed record LookupValues(IrDataId DataId, IrFieldId? FieldId = null);