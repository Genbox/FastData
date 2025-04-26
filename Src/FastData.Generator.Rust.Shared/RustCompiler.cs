using System.Security.Cryptography;
using System.Text;
using static Genbox.FastData.InternalShared.TestHelper;

namespace Genbox.FastData.Generator.Rust.Shared;

public sealed class RustCompiler
{
    private readonly Func<string, string, int> _compile;
    private readonly bool _release;
    private readonly string _rootPath;

    public RustCompiler(bool release, string rootPath)
    {
        _release = release;
        _rootPath = rootPath;

        if (TryRunProcess("rustc.exe", "--version"))
            _compile = CompileRustC;
        else
            throw new InvalidOperationException("No compiler found");
    }

    private int CompileRustC(string src, string dst) =>
        RunProcess("rustc.exe", $"{src} -o {dst} {(_release ? "-C opt-level=3" : "")} -C debuginfo=0 -C link-args=/DEBUG:NONE");

    public string Compile(string fileId, string source)
    {
        string srcFile = Path.Combine(_rootPath, fileId + ".rs");
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