using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.InternalShared.TestClasses;

public interface ITestData
{
    void Generate(Func<string, ICodeGenerator> factory, out GeneratorSpec spec);
    string GetValueLabel(TypeHelper helper);
}