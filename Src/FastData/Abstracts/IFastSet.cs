namespace Genbox.FastData.Abstracts;

public interface IFastSet
{
    bool Contains(string value);
    int Length { get; }
}