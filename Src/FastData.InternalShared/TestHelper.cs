using System.Diagnostics;
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

    public static void CreateOrEmpty(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        else
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.Delete(file);

            foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                Directory.Delete(dir, true);
        }
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
}