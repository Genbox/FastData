using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generator.Framework;

public sealed class TypeMap : ITypeMap
{
    private readonly ITypeDef?[] _index = new ITypeDef?[19];
    private GeneratorEncoding _encoding;

    public TypeMap(IList<ITypeDef> typeSpecs, GeneratorEncoding encoding)
    {
        _encoding = encoding; // This is the generator's default encoding

        for (int i = 0; i < typeSpecs.Count; i++)
        {
            ITypeDef spec = typeSpecs[i];
            byte idx = (byte)spec.DataType;

            //Quick check to see if a language has a duplicate definition for a DataType
            if (_index[idx] != null)
                throw new InvalidOperationException($"Duplicate type spec found for '{spec.DataType}'");

            _index[idx] = spec;
        }
    }

    /// <summary>This is used to override the encoding for dynamic string types. After string analysis, we know if all strings are ASCII, and if that is the case, we can change the encoding in the TypeMap and get more memory efficient code.</summary>
    public void OverrideEncoding(GeneratorEncoding encoding) => _encoding = encoding;

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

    public string GetTypeName(Type type) => Get(type).Name;
}