using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Genbox.FastData.InternalShared;

public static class TestHelper
{
    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(false);
    private static readonly Random _random = new Random(42);

    public static uint[] GetIntegers(IEnumerable<string> input) => input.Select(x => BitConverter.ToUInt32(Encoding.ASCII.GetBytes(x), 0)).ToArray();

    public static string GenerateRandomString(int length)
    {
        char[] data = new char[length];

        for (int i = 0; i < length; i++)
            data[i] = _alphabet[_random.Next(0, _alphabet.Length)];

        return new string(data);
    }

    public static bool TryRunProcess(string application, string args)
    {
        try
        {
            return RunProcess(application, args) == 0;
        }
        catch
        {
            return false;
        }
    }

    public static int RunProcess(string application, string? args = null, string? workingDir = null)
    {
        using Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = application,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = workingDir,
            }
        };

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>
    /// This function is here to help avoid write-fatigue
    /// </summary>
    /// <returns>True if the file was written, false if it was skipped due to identical content</returns>
    public static bool TryWriteFile(string path, string content)
    {
        if (File.Exists(path))
        {
            byte[] oldHash = SHA1.HashData(File.ReadAllBytes(path));
            byte[] newHash = SHA1.HashData(Encoding.UTF8.GetBytes(content));

            if (oldHash.SequenceEqual(newHash))
                return false;
        }

        File.WriteAllText(path, content, _utf8NoBom);
        return true;
    }
}