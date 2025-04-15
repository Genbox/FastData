using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ConditionalStructure : IStructure
{
    public bool TryCreate(object[] data, out IContext? context)
    {
        if (data.Length > ushort.MaxValue)
        {
            context = null;
            return false;
        }

        context = new ConditionalContext(data);
        return true;
    }
}