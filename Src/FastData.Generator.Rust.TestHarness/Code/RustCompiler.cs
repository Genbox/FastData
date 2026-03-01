using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.TestHarness.Code;

public sealed class RustCompiler(string rootPath)
{
    private static ProcessResult CompileRustC(string src, string dst)
    {
        return ProcessHelper.RunProcess("rustc.exe", $"{src} -o {dst} -C opt-level=3 -C debuginfo=0 -C link-args=/DEBUG:NONE");
    }

    public string Compile(string fileId, string source)
    {
        string srcFile = Path.Combine(rootPath, fileId + ".rs");
        string dstFile = Path.Combine(rootPath, fileId + ".exe");

        //If the source hasn't changed, we skip compilation
        if (!FileHelper.TryWriteFile(srcFile, source) && File.Exists(dstFile))
            return dstFile;

        ProcessResult res = CompileRustC(srcFile, dstFile);

        if (res.ExitCode != 0)
        {
            File.Delete(dstFile); // We need to delete the file on failure to avoid returning the cache on next run
            throw new InvalidOperationException($"Failed to compile. Exit code: {res.ExitCode}\nSTDOUT:\n{res.StandardOutput}\nSTDERR:\n{res.StandardError}");
        }

        return dstFile;
    }
}