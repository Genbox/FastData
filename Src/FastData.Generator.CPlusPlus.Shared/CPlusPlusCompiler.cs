using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.InternalShared.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.Shared;

public sealed class CPlusPlusCompiler
{
    private readonly Func<string, string, int> _compile;
    private readonly bool _release;
    private readonly string _rootPath;
    private readonly string _includesPath;
    private readonly string _libsPath;
    private string? _path;

    public CPlusPlusCompiler(bool release)
    {
        _release = release;
        _rootPath = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");
        Directory.CreateDirectory(_rootPath);

        _includesPath = Path.Combine(_rootPath, "Includes");
        Directory.CreateDirectory(_includesPath);

        var benchPath = Path.Combine(_includesPath, "benchmark");
        Directory.CreateDirectory(benchPath);

        CopyResource("benchmark.h", Path.Combine(benchPath, "benchmark.h"));
        CopyResource("export.h", Path.Combine(benchPath, "export.h"));

        _libsPath = Path.Combine(_rootPath, "Libs");
        Directory.CreateDirectory(_libsPath);

        string variant = OperatingSystem.IsWindows() ? "win" : "lin";
        CopyResource($"benchmark-{variant}.lib", Path.Combine(_libsPath, "benchmark.lib"));

        if (HasMSVC())
            _compile = CompileMSVC;
        else
            throw new InvalidOperationException("No compiler found");
    }

    private static void CopyResource(string name, string dst)
    {
        if (File.Exists(dst))
            return;

        const string ns = "Genbox.FastData.Generator.CPlusPlus.Shared.Resources.";
        using Stream? stream = typeof(CPlusPlusCompiler).Assembly.GetManifestResourceStream(ns + name);

        if (stream == null)
            throw new InvalidOperationException("Resource not found");

        using GZipStream gz = new GZipStream(stream, CompressionMode.Decompress);
        using FileStream fs = File.OpenWrite(dst);

        gz.CopyTo(fs);
    }

    private bool HasMSVC()
    {
        string vsWherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (!File.Exists(vsWherePath))
            return false;

        using Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = vsWherePath,
                Arguments = "-latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property productPath",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
        string? productPath = process.StandardOutput.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(productPath))
            return false;

        //productPath points to LaunchDevCmd.bat which is not what we want. We want VsDevCmd.bat in the same folder
        _path = Path.Combine(Path.GetDirectoryName(productPath), "VsDevCmd.bat");
        return true;
    }

    private int CompileMSVC(string src, string dst) =>
        RunProcess("cmd.exe", $"""
                               /c ""{_path}" -arch=x86 && cl.exe "{src}" {(_release ? "/O2 /GL /GS-" : "/O1")} /std:c++17 /DNDEBUG /permissive- /MD /DBENCHMARK_STATIC_DEFINE /I "{_includesPath}" /Fe:"{dst}" "{_libsPath}\benchmark.lib" shlwapi.lib"
                               """);

    public string Compile(string fileId, string source)
    {
        string srcFile = Path.Combine(_rootPath, fileId + ".cpp");
        string dstFile = Path.Combine(_rootPath, fileId + ".exe");

        //If the source hasn't changed, we skip compilation
        if (File.Exists(srcFile) && File.Exists(dstFile))
        {
            byte[] oldHash = SHA1.HashData(File.ReadAllBytes(srcFile));
            byte[] newHash = SHA1.HashData(Encoding.UTF8.GetBytes(source));

            if (oldHash.SequenceEqual(newHash))
                return dstFile;
        }

        File.WriteAllText(srcFile, source);
        int ret = _compile(srcFile, dstFile);

        if (ret != 0)
            throw new InvalidOperationException("Failed to compile. Exit code: " + ret);

        return dstFile;
    }
}