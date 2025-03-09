using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SingleValueCode : IStructure
{
    public IContext Create(object[] data) => new SingleValueContext(data);
}