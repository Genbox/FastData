using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal abstract class CPlusPlusOutputWriter<T> : OutputWriter<T>
{
    protected string GetFieldModifier(bool value) => value ? GetFieldModifier() : "inline static const ";
}