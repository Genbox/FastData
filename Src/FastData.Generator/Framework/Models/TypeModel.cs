namespace Genbox.FastData.Generator.Framework.Models;

public readonly record struct TypeModel(string Name, IReadOnlyList<PropertyModel> Properties);