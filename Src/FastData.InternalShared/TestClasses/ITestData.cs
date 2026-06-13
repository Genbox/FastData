using Genbox.FastData.Generator;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.InternalShared.TestClasses;

public interface ITestData
{
    string Identifier { get; }
    double MaxErrorPercent { get; }
    int TargetIterationTimeMs { get; }
    Type KeyType { get; }
    int QueryCount { get; }
    int WarmupCount { get; }
    int MinSampleCount { get; }
    int MaxSampleCount { get; }
    string Generate(ICodeGenerator generator);
    BenchmarkQuerySet GetQuerySet(TypeMap map);
}