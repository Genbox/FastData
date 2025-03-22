namespace Genbox.FastData.Abstracts;

public interface IHashSpec
{
    Func<string, uint> GetFunction();
    Func<string, string, bool> GetEqualFunction();
    string GetSource();
}