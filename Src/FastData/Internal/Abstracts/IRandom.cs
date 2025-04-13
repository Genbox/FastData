namespace Genbox.FastData.Internal.Abstracts;

internal interface IRandom
{
    double NextDouble();
    int Next();
    int Next(int max);
    int Next(int min, int max);
}