using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Models;

public readonly record struct PrimitiveValue(object Value) : IValueModel;