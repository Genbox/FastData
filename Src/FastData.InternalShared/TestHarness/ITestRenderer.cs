using Genbox.FastData.Enums;

namespace Genbox.FastData.InternalShared.TestHarness;

public interface ITestRenderer
{
    GeneratorEncoding Encoding { get; }
    string ToValueLabel<T>(T value);
    string GetTypeName(Type type);
}
