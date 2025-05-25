using Genbox.FastData.Abstracts;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashStructure<T>
{
    bool TryCreate(T[] data, HashFunc<T> hash, out IContext? context);
}