namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Rendered key type and search expressions for public lookup templates.</summary>
public sealed record RenderedLookupApi(string KeyType, string Contains, string Find, string Found);