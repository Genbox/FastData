using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SwitchCode : IStructure
{
    public IContext Create(object[] data) => new SwitchContext(data);
}