using System.Runtime.CompilerServices;
using Genbox.FastData.HashFunctions;

namespace Genbox.FastData.Helpers;

/// <summary>
/// This helper ensures that we get consistent hashes between compile-time and runtime
/// </summary>
public static class HashHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashObject(object data)
    {
        //We don't want randomized hash codes, so we handle string as a special case
        if (data is string str)
            return HashString(str.AsSpan());

        return (uint)data.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashObjectSeed(object data, uint seed)
    {
        //We don't want randomized hash codes, so we handle string as a special case
        if (data is string str)
            return HashStringSeed(str.AsSpan(), seed);

        uint code = (uint)data.GetHashCode();
        return code ^ seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashString(string data) => DJB2Hash.ComputeHash(data.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashString(ReadOnlySpan<char> data) => DJB2Hash.ComputeHash(data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashStringSeed(ReadOnlySpan<char> data, uint seed) => DJB2Hash.ComputeHash(data, seed);
}