using System.Text;

namespace Genbox.FastData.InternalShared.Helpers;

public static class StringHelper
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

    public static string GenerateMixedEntropyString()
    {
        char[] result = new char[80];

        for (int i = 0; i < 80; i++)
        {
            double noiseValue = PerlinNoise(i * 0.5);
            if (noiseValue > 0.1)
                result[i] = (char)_random.Next(65, 90); // A-Z
            else
                result[i] = '-';
        }

        return new string(result);
    }

    private static double PerlinNoise(double x)
    {
        int xi = (int)x;
        double xf = x - xi;
        double u = xf * xf * (3.0 - (2.0 * xf));
        return Lerp(Noise(xi), Noise(xi + 1), u);
    }

    private static double Noise(int x)
    {
        x = (x << 13) ^ x;
        return unchecked(1.0 - ((((x * ((x * x * 15731) + 789221)) + 1376312589) & 0x7fffffff) / 1073741824.0));
    }

    private static double Lerp(double a, double b, double t) => a + (t * (b - a));
}