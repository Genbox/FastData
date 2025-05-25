using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.InternalShared;

public interface ITestData
{
    void Generate(Func<string, ICodeGenerator> factory, out GeneratorSpec spec);
    string GetValueLabel(TypeHelper helper);
}

public interface ITestVector;
