using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Genbox.FastData.InternalShared.Misc;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Genbox.FastData.InternalShared.Helpers;

public sealed class DockerManager : IAsyncDisposable
{
    private const string WorkDir = "/work";
    private const string DefaultContainerPrefix = "fastdata";
    private readonly DockerClient _client = new DockerClientConfiguration().CreateClient();
    private readonly ConcurrentDictionary<string, string> _containersByImage = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
    private readonly string _containerPrefix;

    public DockerManager(string containerPrefix = DefaultContainerPrefix)
    {
        if (string.IsNullOrWhiteSpace(containerPrefix))
            throw new ArgumentException("Container prefix must be provided.", nameof(containerPrefix));

        _containerPrefix = containerPrefix;
        RemoveAllManagedContainersAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<ProcessResult> RunInContainerAsync(string imageId, string workDir, string command, CancellationToken cancellationToken = default)
    {
        await EnsureImageAsync(imageId, cancellationToken).ConfigureAwait(false);

        string containerId = await GetOrCreateContainerAsync(imageId, workDir, cancellationToken).ConfigureAwait(false);
        (string standardOutput, string standardError, int exitCode) = await ExecInContainerAsync(containerId, command, cancellationToken).ConfigureAwait(false);

        return new ProcessResult(exitCode, standardOutput, standardError);
    }

    public async ValueTask DisposeAsync()
    {
        await RemoveAllManagedContainersAsync(CancellationToken.None).ConfigureAwait(false);
        _containersByImage.Clear();
        _client.Dispose();
    }

    private async Task EnsureImageAsync(string imageId, CancellationToken cancellationToken)
    {
        try
        {
            await _client.Images.InspectImageAsync(imageId, cancellationToken).ConfigureAwait(false);
        }
        catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            (string repo, string tag) = SplitImage(imageId);
            ImagesCreateParameters parameters = new ImagesCreateParameters
            {
                FromImage = repo,
                Tag = tag
            };

            await _client.Images.CreateImageAsync(parameters, authConfig: null, progress: new Progress<JSONMessage>(), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> GetOrCreateContainerAsync(string imageId, string workDir, CancellationToken cancellationToken)
    {
        if (_containersByImage.TryGetValue(imageId, out string? containerId))
            return containerId;

        string newContainerId = await CreatePersistentContainerAsync(imageId, workDir, cancellationToken).ConfigureAwait(false);
        _containersByImage.TryAdd(imageId, newContainerId);
        return newContainerId;
    }

    private async Task<string> CreatePersistentContainerAsync(string imageId, string workDir, CancellationToken cancellationToken)
    {
        string name = _containerPrefix + "-" + Guid.NewGuid().ToString("N");
        CreateContainerResponse created = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = imageId,
            Name = name,
            WorkingDir = WorkDir,
            Cmd = ["/bin/sh", "-c", "sleep infinity"],
            AttachStdout = false,
            AttachStderr = false,
            Tty = false,
            HostConfig = new HostConfig
            {
                AutoRemove = false,
                Binds = new List<string> { $"{workDir}:{WorkDir}" }
            }
        }, cancellationToken).ConfigureAwait(false);

        await _client.Containers.StartContainerAsync(created.ID, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false);

        return created.ID;
    }

    private async Task<(string StandardOutput, string StandardError, int ExitCode)> ExecInContainerAsync(string containerId, string command, CancellationToken cancellationToken)
    {
        ContainerExecCreateParameters execCreateParameters = new ContainerExecCreateParameters
        {
            AttachStdout = true,
            AttachStderr = true,
            Cmd = ["/bin/sh", "-c", command],
            WorkingDir = WorkDir
        };

        ContainerExecCreateResponse created = await _client.Exec.ExecCreateContainerAsync(containerId, execCreateParameters, cancellationToken).ConfigureAwait(false);
        using MultiplexedStream stream = await _client.Exec.StartAndAttachContainerExecAsync(created.ID, false, cancellationToken).ConfigureAwait(false);

        await using MemoryStream stdOut = new MemoryStream();
        await using MemoryStream stdErr = new MemoryStream();
        await stream.CopyOutputToAsync(Stream.Null, stdOut, stdErr, cancellationToken).ConfigureAwait(false);

        ContainerExecInspectResponse execInspect = await _client.Exec.InspectContainerExecAsync(created.ID, cancellationToken).ConfigureAwait(false);

        string standardOutput = Encoding.UTF8.GetString(stdOut.ToArray());
        string standardError = Encoding.UTF8.GetString(stdErr.ToArray());
        return (standardOutput, standardError, checked((int)execInspect.ExitCode));
    }

    private async Task RemoveAllManagedContainersAsync(CancellationToken cancellationToken)
    {
        IList<ContainerListResponse> containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true
        }, cancellationToken).ConfigureAwait(false);

        foreach (ContainerListResponse container in containers)
        {
            if (!IsManagedContainer(container))
                continue;

            await RemoveContainerAsync(container.ID, cancellationToken).ConfigureAwait(false);
        }
    }

    private bool IsManagedContainer(ContainerListResponse container)
    {
        if (container.Names == null || container.Names.Count == 0)
            return false;

        foreach (string name in container.Names)
        {
            if (name.StartsWith("/" + _containerPrefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private async Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        try
        {
            await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
            {
                Force = true,
                RemoveVolumes = true
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Do nothing
        }
    }

    private static (string Repo, string Tag) SplitImage(string imageId)
    {
        int lastColon = imageId.LastIndexOf(':');
        if (lastColon > 0 && lastColon < imageId.Length - 1 && imageId.IndexOf('/', StringComparison.Ordinal) < lastColon)
            return (imageId[..lastColon], imageId[(lastColon + 1)..]);

        return (imageId, "latest");
    }
}