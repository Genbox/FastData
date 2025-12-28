namespace Genbox.FastData.InternalShared.TestHarness;

public interface ITestRenderer
{
    string ToValueLabel<T>(T value);
    string GetTypeName(Type type);
}
