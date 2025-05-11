using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IEarlyExitHandler
{
    string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits);
}