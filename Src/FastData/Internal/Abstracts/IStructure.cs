using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Abstracts;

internal interface IStructure
{
    IContext Create(object[] data);
}