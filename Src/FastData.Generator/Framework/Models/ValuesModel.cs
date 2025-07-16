using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Models;

public readonly record struct ValuesModel(ICollection<TypeModel> Types, ICollection<IValueModel> Values, ITypeReference ElementType);