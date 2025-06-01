using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Genbox.FastData.InternalShared.Helpers;

public static class TestHelper
{
    private const string _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(false);

    public static void CreateOrEmptyDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);
    }

    public static string GenerateRandomString(Random rng, int length)
    {
        char[] data = new char[length];

        for (int i = 0; i < length; i++)
        {
            data[i] = _alphabet[rng.Next(0, _alphabet.Length)];
        }

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
                WorkingDirectory = workingDir
            }
        };

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>This function is here to help avoid write-fatigue</summary>
    /// <returns>True if the file was written, false if it was skipped due to identical content</returns>
    public static bool TryWriteFile(string path, string content)
    {
        if (File.Exists(path))
        {
            byte[] oldHash = SHA1.HashData(File.ReadAllBytes(path));
            byte[] newHash = SHA1.HashData(_utf8NoBom.GetBytes(content));

            if (oldHash.SequenceEqual(newHash))
                return false;
        }

        File.WriteAllText(path, content, _utf8NoBom);
        return true;
    }
}