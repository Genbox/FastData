namespace Genbox.FastData.Internal.Abstracts;

internal interface ICode
{
    bool TryCreate();
    string Generate();
}