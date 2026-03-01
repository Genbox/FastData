using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness.Code;

public sealed class ClangCompiler
{
    private readonly string _rootPath;
    internal const string CompilerPath = @"C:\clang+llvm-22.1.0-x86_64-pc-windows-msvc\bin\clang++.exe";

    public ClangCompiler(string rootDir)
    {
        _rootPath = rootDir;

        if (!ProcessHelper.TryRunProcess(CompilerPath, "--version"))
            throw new InvalidOperationException("No compiler found");
    }

    public string Compile(string fileId, string source)
    {
        string srcFile = Path.Combine(_rootPath, fileId + ".cpp");
        string dstFile = Path.Combine(_rootPath, fileId + ".exe");

        //If the source hasn't changed, we skip compilation
        if (!FileHelper.TryWriteFile(srcFile, source) && File.Exists(dstFile))
            return dstFile;

        ProcessResult res = ProcessHelper.RunProcess(CompilerPath, $"\"{srcFile}\" -std=c++17 -O3 -DNDEBUG -o \"{dstFile}\"");

        if (res.ExitCode != 0)
        {
            File.Delete(dstFile); // We need to delete the file on failure to avoid returning the cache on next run
            throw new InvalidOperationException($"Failed to compile. Exit code: {res.ExitCode}\nSTDOUT:\n{res.StandardOutput}\nSTDERR:\n{res.StandardError}");
        }

        return dstFile;
    }
}