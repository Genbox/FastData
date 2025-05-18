namespace Genbox.FastData.Specs;

public delegate ulong HashFunc<in T>(T obj);

public delegate ulong HashFunc(ref byte obj, int length);