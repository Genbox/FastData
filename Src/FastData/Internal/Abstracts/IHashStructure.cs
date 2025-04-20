using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IHashStructure
{
    bool TryCreate(object[] data, HashFunc hash, out IContext? context);
}