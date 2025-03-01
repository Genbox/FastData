namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashSpec
{
    Func<string, uint> GetFunction();
    string GetSource();
}