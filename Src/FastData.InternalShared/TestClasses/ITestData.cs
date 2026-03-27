using Genbox.FastData.Generator;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.InternalShared.TestClasses;

public interface ITestData
{
    string Identifier { get; }
    int WarmupIterations { get; }
    int WorkIterations { get; }
    int QueryCount { get; }
    string Generate(ICodeGenerator generator);
    string GetRandomKey(TypeMap map);
}