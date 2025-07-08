using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework;

public sealed class TypeMap
{
    private readonly ITypeDef?[] _index = new ITypeDef?[19];

    public TypeMap(IList<ITypeDef> typeSpecs)
    {
        for (int i = 0; i < typeSpecs.Count; i++)
        {
            ITypeDef spec = typeSpecs[i];
            byte idx = (byte)spec.DataType;

            //Quick check to see if a language have a duplicate definition for a DataType
            if (_index[idx] != null)
                throw new InvalidOperationException($"Duplicate type spec found for '{spec.DataType}'");

            _index[idx] = spec;
        }
    }

    public string GetName(Type t)
    {
        ITypeDef? res = _index[(int)Type.GetTypeCode(t)];
        return res == null ? t.Name : res.Name;
    }

    public ITypeDef<T> Get<T>() => (ITypeDef<T>)GetDef(typeof(T));

    public ITypeDef GetDef(Type t)
    {
        ITypeDef? res = _index[(int)Type.GetTypeCode(t)];

        if (res == null)
            throw new InvalidOperationException("No type spec was found for " + t.Name);

        return res;
    }
}