using Genbox.FastData.Generator.Ir.Operations;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A callable's generated name and optional native builtin implementation.</summary>
public sealed record PreparedCall(string Name, IrBuiltinOperation Operation);