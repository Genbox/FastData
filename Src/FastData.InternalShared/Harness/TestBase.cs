using Genbox.FastData.InternalShared.TestClasses;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class TestBase<T>(T bootstrap) : TestBase(bootstrap) where T : BootstrapBase
{
    public T Bootstrap { get; } = bootstrap;
}

public abstract class TestBase(BootstrapBase bootstrap) : HarnessBase(bootstrap), IXunitSerializable
{
    public abstract string RenderContains<TKey>(GeneratorSpec spec, TKey[] present, TKey[] notPresent);
    public abstract string RenderTryLookup<TKey, TValue>(GeneratorSpec spec, TestVector<TKey, TValue> vector) where TValue : notnull;

    public abstract int Run(string fileId, string source);

    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Name), Name);
    public void Deserialize(IXunitSerializationInfo info) => info.GetValue<string>(nameof(Name));
}