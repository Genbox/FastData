using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class HarnessBase(BootstrapBase bootstrap, DockerManager dockerManager)
{
    public string Name => bootstrap.Name;
    public ICodeGenerator Generator => bootstrap.Generator;

    protected async Task<ProcessResult> RunAsync(string program, string id, bool useCache, CancellationToken cancellationToken = default)
    {
        string fileName = id + bootstrap.Ext;
        string fullPath = Path.Combine(bootstrap.RootDir, fileName);

        string command = string.Format(CultureInfo.InvariantCulture, bootstrap.CommandTemplate, fileName, id);

        bool cacheEnabled = bootstrap.Type == HarnessType.Test && useCache;
        string hashFile = string.Empty;
        string programHash = string.Empty;

        if (cacheEnabled)
        {
            hashFile = fullPath + ".fastdata.hash";
            programHash = ComputeHash(program);

            if (File.Exists(fullPath) && await HashMatchesAsync(hashFile, programHash, cancellationToken).ConfigureAwait(false))
                return new ProcessResult(1, string.Empty, string.Empty);
        }

        await File.WriteAllTextAsync(fullPath, program, cancellationToken).ConfigureAwait(false);

        ProcessResult res = await dockerManager.RunInContainerAsync(bootstrap.DockerImage, bootstrap.RootDir, command, cancellationToken).ConfigureAwait(false);

        if (res.ExitCode != 0 && HasError(res.StandardError))
            throw new InvalidOperationException($"Failed to compile or run. Exit code: {res.ExitCode}\nSTDERR:\n{res.StandardError}");

        if (cacheEnabled && res.ExitCode == 1)
            await File.WriteAllTextAsync(hashFile, programHash, cancellationToken).ConfigureAwait(false);

        return res;
    }

    public override string ToString() => bootstrap.Name;

    private static bool HasError(string standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
            return false;

        return standardError.Contains("error", StringComparison.OrdinalIgnoreCase);
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