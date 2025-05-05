using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Contexts;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public class ArrayContext<T>(T[] data) : DefaultContext<T>(data);