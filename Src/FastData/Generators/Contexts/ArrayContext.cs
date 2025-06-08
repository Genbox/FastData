namespace Genbox.FastData.Generators.Contexts;

public sealed class ArrayContext<T>(T[] data) : DefaultContext<T>(data);