namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Source expressions for an enumeration cursor and its current element.</summary>
public sealed record RenderedEnumeration(string Name, string Type, string InitialPosition, string Start, string Advance, string HasCurrent, string Current);