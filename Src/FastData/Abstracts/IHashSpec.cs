namespace Genbox.FastData.Abstracts;

public interface IHashSpec
{
    Func<string, uint> GetFunction();
    string GetSource();
}