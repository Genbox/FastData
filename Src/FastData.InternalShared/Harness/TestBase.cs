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

    public async Task<int> RunContainsAsync<TKey>(string source, string id, TKey[] present, TKey[] notPresent, CancellationToken cancellationToken = default) => await RunLocalAsync(RenderContains(source, present, notPresent), id, cancellationToken).ConfigureAwait(false);

    public async Task<int> RunTryLookupAsync<TKey, TValue>(string source, string id, TKey[] present, TValue[] presentValues, TKey[] notPresent, CancellationToken cancellationToken = default) => await RunLocalAsync(RenderTryLookup(source, present, presentValues, notPresent), id, cancellationToken).ConfigureAwait(false);

    private async Task<int> RunLocalAsync(string program, string id, CancellationToken cancellationToken)
    {
        ProcessResult res = await RunAsync(program, id, cancellationToken).ConfigureAwait(false);
        return res.ExitCode;
    }
}