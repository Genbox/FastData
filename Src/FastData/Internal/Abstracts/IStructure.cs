using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure
{
    bool TryCreate(object[] data, out IContext? context);
}