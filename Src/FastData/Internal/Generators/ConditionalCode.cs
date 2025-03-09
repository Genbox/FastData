using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class ConditionalCode : IStructure
{
    public IContext Create(object[] data) => new ConditionalContext(data);
}