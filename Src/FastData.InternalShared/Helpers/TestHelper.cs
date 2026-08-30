using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.InternalShared.Helpers;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This helper generates non-security-sensitive test data.")]
internal static class TestHelper
{
    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRandomString(Random rng, int length)
    {
        char[] data = new char[length];

        for (int i = 0; i < length; i++)
            data[i] = _alphabet[rng.Next(0, _alphabet.Length)];

        return new string(data);
    }
}