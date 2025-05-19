namespace Genbox.FastData.Misc;

public delegate ulong HashFunc<in T>(T obj);

public delegate ulong ArrayHashFunc(ref byte obj, int length);