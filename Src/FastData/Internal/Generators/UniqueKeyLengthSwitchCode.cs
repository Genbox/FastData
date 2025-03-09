using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class UniqueKeyLengthSwitchCode : IStructure
{
    public IContext Create(object[] data) => new UniqueKeyLengthSwitchContext(data);
}