namespace Genbox.FastData.Generators.StringHash.Framework;

/// <summary>Represents a hash function over encoded string data.</summary>
/// <param name="value">The encoded value.</param>
/// <param name="length">The logical input length.</param>
/// <returns>The 64-bit hash code.</returns>
public delegate ulong StringHashFunc(byte[] value, int length);