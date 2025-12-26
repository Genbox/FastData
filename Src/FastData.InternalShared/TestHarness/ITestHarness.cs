using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.TestClasses;
using Xunit.Sdk;

namespace Genbox.FastData.InternalShared.TestHarness;

public interface ITestHarness : IXunitSerializable
{
    string Name { get; }

    ICodeGenerator CreateGenerator(string id);
    ITestRenderer CreateRenderer(GeneratorSpec spec);

    string RenderContainsProgram<T>(GeneratorSpec spec, ITestRenderer renderer, T[] present, T[] notPresent);
    string RenderTryLookupProgram<TKey, TValue>(GeneratorSpec spec, ITestRenderer renderer, TestVector<TKey, TValue> vector) where TValue : notnull;

    int Run(string fileId, string source);
}
