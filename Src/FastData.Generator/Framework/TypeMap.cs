using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generator.Framework;

public sealed class TypeMap : ITypeMap
{
    private readonly GeneratorEncoding _encoding;
    private readonly ITypeDef?[] _index = new ITypeDef?[19];

    public TypeMap(IList<ITypeDef> typeSpecs, GeneratorEncoding encoding)
    {
        _encoding = encoding;
        for (int i = 0; i < typeSpecs.Count; i++)
        {
            ITypeDef spec = typeSpecs[i];
            byte idx = (byte)spec.KeyType;

            //Quick check to see if a language has a duplicate definition for a DataType
            if (_index[idx] != null)
                throw new InvalidOperationException($"Duplicate type spec found for '{spec.KeyType}'");

            _index[idx] = spec;
        }
    }

    public string GetNull() => _index[0].PrintObj(this, null);

    public ITypeDef<T> Get<T>() => (ITypeDef<T>)Get(typeof(T));

    public ITypeDef Get(Type type)
    {
        ITypeDef? res = _index[(int)Type.GetTypeCode(type)];

        if (res == null)
            throw new InvalidOperationException("No type spec was found for " + type.Name);

        if (res is DynamicStringTypeDef dyn)
            res = dyn.Get(_encoding).StringTypeDef;

        return res;
    }

    public string GetTypeName(Type type)
    {
        ITypeDef res = Get(type);

        if (res is ObjectTypeDef)
            return type.Name;

        return res.Name;
    }
}