namespace Genbox.FastData.Contexts;

public sealed class ConditionalContext<T>(T[] data) : DefaultContext<T>(data);