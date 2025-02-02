namespace Genbox.FastData.Internal.Analysis.Genetic;

internal interface IHashSpec
{
    Func<string, uint> GetFunction();
    string Construct();
}