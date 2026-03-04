using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class HarnessBase(BootstrapBase bootstrap, DockerManager dockerManager)
{
    public string Name => bootstrap.Name;
    public string RootDir => bootstrap.RootDir;
    public string CommandTemplate => bootstrap.CommandTemplate;
    public ICodeGenerator Generator => bootstrap.Generator;

    protected async Task<ProcessResult> RunAsync(string program, string id, CancellationToken cancellationToken = default)
    {
        string fileName = id + bootstrap.Ext;
        string fullPath = Path.Combine(RootDir, fileName);

        string command = string.Format(CultureInfo.InvariantCulture, CommandTemplate, fileName, id);

        if (bootstrap.Type == HarnessType.Test)
            return await RunTestAsync(fullPath, program, command, cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(fullPath, program, cancellationToken).ConfigureAwait(false);

        ProcessResult res = await dockerManager.RunInContainerAsync(bootstrap.DockerImage, RootDir, command, cancellationToken).ConfigureAwait(false);

        if (res.ExitCode != 0 && HasError(res.StandardError))
            throw new InvalidOperationException($"Failed to compile or run. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        return res;
    }

    public override string ToString() => bootstrap.Name;

    private static bool HasError(string standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
            return false;

        return standardError.Contains("error", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ProcessResult> RunTestAsync(string fullPath, string program, string command, CancellationToken cancellationToken)
    {
        string hashFile = fullPath + ".fastdata.hash";
        string programHash = ComputeHash(program);

        if (File.Exists(fullPath) && await HashMatchesAsync(hashFile, programHash, cancellationToken).ConfigureAwait(false))
            return new ProcessResult(1, string.Empty, string.Empty);

        await File.WriteAllTextAsync(fullPath, program, cancellationToken).ConfigureAwait(false);

        ProcessResult res = await dockerManager.RunInContainerAsync(bootstrap.DockerImage, RootDir, command, cancellationToken).ConfigureAwait(false);
        if (res.ExitCode != 0 && HasError(res.StandardError))
            throw new InvalidOperationException($"Failed to compile or run. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        if (res.ExitCode == 1)
            await File.WriteAllTextAsync(hashFile, programHash, cancellationToken).ConfigureAwait(false);

        return res;
    }

    private static async Task<bool> HashMatchesAsync(string hashFile, string programHash, CancellationToken cancellationToken)
    {
        if (!File.Exists(hashFile))
            return false;

        string storedHash = await File.ReadAllTextAsync(hashFile, cancellationToken).ConfigureAwait(false);
        return string.Equals(storedHash, programHash, StringComparison.Ordinal);
    }

    private static string ComputeHash(string program)
    {
        byte[] data = Encoding.UTF8.GetBytes(program);
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }
}