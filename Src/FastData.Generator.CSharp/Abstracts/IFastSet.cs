namespace Genbox.FastData.Generator.CSharp.Abstracts;

public interface IFastSet<in T>
{
    bool Contains(T value);
    int Length { get; }
}