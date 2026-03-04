using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.TestHarness.Runner.Tests;

public sealed class DockerCSharpFixture : IAsyncDisposable
{
    public DockerManager DockerManager { get; } = new DockerManager("fastdata-csharp");

    public async ValueTask DisposeAsync()
    {
        await DockerManager.DisposeAsync().ConfigureAwait(false);
    }
}

public sealed class DockerCPlusPlusFixture : IAsyncDisposable
{
    public DockerManager DockerManager { get; } = new DockerManager("fastdata-cpp");

    public async ValueTask DisposeAsync()
    {
        await DockerManager.DisposeAsync().ConfigureAwait(false);
    }
}

public sealed class DockerRustFixture : IAsyncDisposable
{
    public DockerManager DockerManager { get; } = new DockerManager("fastdata-rust");

    public async ValueTask DisposeAsync()
    {
        await DockerManager.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition("Docker-CSharp")]
public sealed class DockerCSharpCollection : ICollectionFixture<DockerCSharpFixture>;

[CollectionDefinition("Docker-CPlusPlus")]
public sealed class DockerCPlusPlusCollection : ICollectionFixture<DockerCPlusPlusFixture>;

[CollectionDefinition("Docker-Rust")]
public sealed class DockerRustCollection : ICollectionFixture<DockerRustFixture>;