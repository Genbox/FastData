using Genbox.FastData.Generator.Ir.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.Ir.Harness.Runner;

internal sealed class DockerHarness : IAsyncDisposable
{
    private readonly DockerManager _manager = new DockerManager("fastdata-ir");

    public ValueTask DisposeAsync() => _manager.DisposeAsync();

    public async Task BuildAsync(HarnessBackend backend, string directory, string id, bool benchmark, CancellationToken cancellationToken)
    {
        // Linux shell continuations require LF even when raw strings come from a Windows checkout.
        string command = backend.BuildCommand(id, benchmark).ReplaceLineEndings("\n");
        ProcessResult result = await _manager.RunInContainerAsync(backend.Image, directory, command, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory, id + ".build.log"), result.StandardOutput + result.StandardError, cancellationToken);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Compilation failed ({result.ExitCode}): {result.StandardOutput}{result.StandardError}");
    }

    public async Task<ProcessResult> RunAsync(HarnessBackend backend, string directory, string id, string arguments, CancellationToken cancellationToken)
    {
        string command = backend.RunCommand(id, arguments).ReplaceLineEndings("\n");
        ProcessResult result = await _manager.RunInContainerAsync(backend.Image, directory, command, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory, id + ".run.log"), result.StandardOutput + result.StandardError, cancellationToken);
        return result;
    }
}