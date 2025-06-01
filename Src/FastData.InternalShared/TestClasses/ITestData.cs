using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.InternalShared.TestClasses;

public interface ITestData
{
    void Generate(Func<string, ICodeGenerator> factory, out GeneratorSpec spec);
    string GetValueLabel(TypeHelper helper);
}