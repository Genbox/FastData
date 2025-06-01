namespace Genbox.FastData.Contexts;

public sealed class ArrayContext<T>(T[] data) : DefaultContext<T>(data);