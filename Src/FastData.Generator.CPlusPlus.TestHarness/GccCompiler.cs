using System.IO.Compression;
using Genbox.FastData.InternalShared.Helpers;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class GccCompiler
{
    private readonly string _compiler;
    private readonly string _includesPath;
    private readonly string _libsPath;
    private readonly bool _linkBenchmark;
    private readonly bool _release;
    private readonly string _rootPath;

    public GccCompiler(bool release, string rootDir, bool linkBenchmark)
    {
        _release = release;
        _rootPath = rootDir;
        _linkBenchmark = linkBenchmark;

        _includesPath = Path.Combine(_rootPath, "Includes");
        Directory.CreateDirectory(_includesPath);

        string benchPath = Path.Combine(_includesPath, "benchmark");
        Directory.CreateDirectory(benchPath);

        CopyResource("benchmark.h", Path.Combine(benchPath, "benchmark.h"));
        CopyResource("export.h", Path.Combine(benchPath, "export.h"));

        _libsPath = Path.Combine(_rootPath, "Libs");
        Directory.CreateDirectory(_libsPath);

        if (!TryGetCompiler(out _compiler))
            throw new InvalidOperationException("No compiler found");
    }

    private static void CopyResource(string name, string dst)
    {
        if (File.Exists(dst))
            return;

        const string ns = "Genbox.FastData.Generator.CPlusPlus.TestHarness.Resources.";
        using Stream? stream = typeof(GccCompiler).Assembly.GetManifestResourceStream(ns + name + ".gz");

        if (stream == null)
            throw new InvalidOperationException("Resource not found");

        using GZipStream gz = new GZipStream(stream, CompressionMode.Decompress);
        using FileStream fs = File.OpenWrite(dst);

        gz.CopyTo(fs);
    }

    private static bool TryGetCompiler(out string compiler)
    {
        if (TryRunProcess("g++.exe", "--version"))
        {
            compiler = "g++.exe";
            return true;
        }

        if (TryRunProcess("g++", "--version"))
        {
            compiler = "g++";
            return true;
        }

        compiler = string.Empty;
        return false;
    }

    private ProcessResult CompileGcc(string src, string dst)
    {
        string args = BuildArgs(src, dst);
        return RunProcess(_compiler, args);
    }

    private string BuildArgs(string src, string dst)
    {
        List<string> args = new List<string>
        {
            $"\"{src}\"",
            "-std=c++17",
            _release ? "-O2" : "-O1",
            "-DNDEBUG",
            $"-I \"{_includesPath}\""
        };

        if (_linkBenchmark)
        {
            args.Add("-DBENCHMARK_STATIC_DEFINE");
            args.Add($"-L \"{_libsPath}\"");
            args.Add("-lbenchmark");

            if (OperatingSystem.IsWindows())
                args.Add("-lshlwapi");
            else
                args.Add("-pthread");
        }

        args.Add($"-o \"{dst}\"");

        return string.Join(" ", args);
    }

    public string Compile(string fileId, string source)
    {
        string srcFile = Path.Combine(_rootPath, fileId + ".cpp");
        string dstFile = Path.Combine(_rootPath, OperatingSystem.IsWindows() ? fileId + ".exe" : fileId);

        //If the source hasn't changed, we skip compilation
        if (!TryWriteFile(srcFile, source) && File.Exists(dstFile))
            return dstFile;

        ProcessResult res = CompileGcc(srcFile, dstFile);

        if (res.ExitCode != 0)
        {
            File.Delete(dstFile); // We need to delete the file on failure to avoid returning the cache on next run
            throw new InvalidOperationException($"Failed to compile. Exit code: {res.ExitCode}\nSTDOUT:\n{res.StandardOutput}\nSTDERR:\n{res.StandardError}");
        }

        return dstFile;
    }
}
