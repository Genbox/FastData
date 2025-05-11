namespace Genbox.FastData.Internal.Abstracts;

internal interface IHasMinMax<out T>
{
    T MinValue { get; }
    T MaxValue { get; }
}