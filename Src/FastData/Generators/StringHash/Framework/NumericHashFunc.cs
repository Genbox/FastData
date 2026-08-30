namespace Genbox.FastData.Generators.StringHash.Framework;

/// <summary>Represents a numeric-key hash function.</summary>
/// <typeparam name="T">The key type.</typeparam>
/// <param name="obj">The key to hash.</param>
/// <returns>The 64-bit hash code.</returns>
public delegate ulong NumericHashFunc<in T>(T obj);