namespace Genbox.FastData.Generator.CSharp.Abstracts;

public interface IFastSet<in T>
{
    int Length { get; }
    bool Contains(T value);
}