using Genbox.FastData.InternalShared.Helpers;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustCompiler
{
    private readonly Func<string, string, ProcessResult> _compile;
    private readonly bool _release;
    private readonly string _rootPath;

    public RustCompiler(bool release, string rootPath)
    {
        _release = release;
        _rootPath = rootPath;
        Directory.CreateDirectory(rootPath);

        if (TryRunProcess("rustc.exe", "--version"))
            _compile = CompileRustC;
        else
            throw new InvalidOperationException("No compiler found");
    }

    private ProcessResult CompileRustC(string src, string dst) =>
        RunProcess("rustc.exe", $"{src} -o {dst} {(_release ? "-C opt-level=3" : "")} -C debuginfo=0 -C link-args=/DEBUG:NONE");

    public string Compile(string fileId, string source)
    {
        string srcFile = Path.Combine(_rootPath, fileId + ".rs");
        string dstFile = Path.Combine(_rootPath, fileId + ".exe");

        //If the source hasn't changed, we skip compilation
        if (!TryWriteFile(srcFile, source) && File.Exists(dstFile))
            return dstFile;

        ProcessResult res = _compile(srcFile, dstFile);

        if (res.ExitCode != 0)
        {
            File.Delete(dstFile); // We need to delete the file on failure to avoid returning the cache on next run
            throw new InvalidOperationException($"Failed to compile. Exit code: {res.ExitCode}\nSTDOUT:\n{res.StandardOutput}\nSTDERR:\n{res.StandardError}");
        }

        return dstFile;
    }
}
