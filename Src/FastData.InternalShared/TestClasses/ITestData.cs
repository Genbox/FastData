using Genbox.FastData.Generator;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.InternalShared.TestClasses;

public interface ITestData
{
    string Identifier { get; }
    int WorkIterations { get; }
    int QueryCount { get; }
    int WarmupCount { get; }
    int SampleCount { get; }
    string Generate(ICodeGenerator generator);
    string[] GetQuerySet(TypeMap map);
}