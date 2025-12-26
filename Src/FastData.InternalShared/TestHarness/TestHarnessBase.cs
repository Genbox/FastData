using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.TestClasses;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestHarness;

public abstract class TestHarnessBase(string name) : ITestHarness
{
    public string Name => name;

    public abstract ICodeGenerator CreateGenerator(string id);
    public abstract ITestRenderer CreateRenderer(GeneratorSpec spec);

    public abstract string RenderContainsProgram<T>(GeneratorSpec spec, ITestRenderer renderer, T[] present, T[] notPresent);
    public abstract string RenderTryLookupProgram<TKey, TValue>(GeneratorSpec spec, ITestRenderer renderer, TestVector<TKey, TValue> vector) where TValue : notnull;

    public abstract int Run(string fileId, string source);

    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Name), Name);
    public void Deserialize(IXunitSerializationInfo info) => info.GetValue<string>(nameof(Name));

    public override string ToString() => Name;
}