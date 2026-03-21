using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.Misc;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class TestBase<T>(T bootstrap, DockerManager manager) : TestBase(bootstrap, manager) where T : BootstrapBase
{
    protected T Bootstrap { get; } = bootstrap;
}

public abstract class TestBase(BootstrapBase bootstrap, DockerManager dockerManager) : HarnessBase(bootstrap, dockerManager), IXunitSerializable
{
    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Name), Name);
    public void Deserialize(IXunitSerializationInfo info) => info.GetValue<string>(nameof(Name));
    protected abstract string RenderContains<TKey>(string source, TKey[] present, TKey[] notPresent);
    protected abstract string RenderTryLookup<TKey, TValue>(string source, TKey[] present, TValue[] presentValues, TKey[] notPresent);

    public async Task<int> RunContainsAsync<TKey>(string source, string id, TKey[] present, TKey[] notPresent, CancellationToken cancellationToken = default) => await RunProgramAsync(RenderContains(source, present, notPresent), id, true, cancellationToken).ConfigureAwait(false);

    public async Task<int> RunTryLookupAsync<TKey, TValue>(string source, string id, TKey[] present, TValue[] presentValues, TKey[] notPresent, CancellationToken cancellationToken = default) => await RunProgramAsync(RenderTryLookup(source, present, presentValues, notPresent), id, true, cancellationToken).ConfigureAwait(false);

    public async Task<int> RunProgramAsync(string program, string id, bool useCache, CancellationToken cancellationToken = default) => (await RunAsync(program, id, useCache, cancellationToken).ConfigureAwait(false)).ExitCode;
}