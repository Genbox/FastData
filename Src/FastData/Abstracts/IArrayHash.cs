using Genbox.FastData.Misc;

namespace Genbox.FastData.Abstracts;

public interface IArrayHash
{
    ArrayHashFunc GetHashFunction();
}