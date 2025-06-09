namespace Genbox.FastData.Generator.Abstracts;

public interface IOutputWriter<T>
{
    public string Generate(ReadOnlySpan<T> data);
}