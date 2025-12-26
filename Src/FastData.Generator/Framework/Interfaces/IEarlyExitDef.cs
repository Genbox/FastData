using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IEarlyExitDef
{
    string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits, MethodType methodType, bool ignoreCase, GeneratorEncoding encoding);
}