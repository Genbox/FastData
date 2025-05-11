using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework;

public sealed class TypeMap
{
    private readonly ITypeDef?[] _index = new ITypeDef?[19];

    public TypeMap(IList<ITypeDef> typeSpecs)
    {
        for (int i = 0; i < typeSpecs.Count; i++)
        {
            ITypeDef? spec = typeSpecs[i];
            _index[(byte)spec.DataType] = spec;
        }
    }

    public ITypeDef<T> Get<T>()
    {
        ITypeDef<T>? res = (ITypeDef<T>?)_index[(int)Type.GetTypeCode(typeof(T))];

        if (res == null)
            throw new InvalidOperationException("No type spec was found for " + typeof(T).Name);

        return res;
    }
}