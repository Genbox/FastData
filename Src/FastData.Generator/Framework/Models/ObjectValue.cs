using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Models;

public readonly record struct ObjectValue(string TypeName, IReadOnlyDictionary<string, IValueModel> Properties) : IValueModel;