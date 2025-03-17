using System.Text;

namespace Genbox.FastData.InternalShared;

public static class TestHelper
{
    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly Random _random = new Random(42);

    public static uint[] GetIntegers(IEnumerable<string> input) => input.Select(x => BitConverter.ToUInt32(Encoding.ASCII.GetBytes(x), 0)).ToArray();

    public static string GenerateRandomString(int length)
    {
        char[] data = new char[length];

        for (int i = 0; i < length; i++)
            data[i] = _alphabet[_random.Next(0, _alphabet.Length)];

        return new string(data);
    }
}