using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public sealed class TypeMap
{
    private readonly ITypeSpec?[] _index = new ITypeSpec?[18];
    private readonly ITypeSpec _default;
    private readonly ITypeSpec _arrayType;

    public TypeMap(IList<ITypeSpec> typeSpecs)
    {
        for (int i = 0; i < typeSpecs.Count; i++)
        {
            ITypeSpec? spec = typeSpecs[i];
            _index[(byte)spec.DataType] = spec;

            if (spec is IIntegerTypeSpec n)
            {
                if (n.Flags.HasFlag(IntegerTypeFlags.Default))
                    _default = spec;

                if (n.Flags.HasFlag(IntegerTypeFlags.ArraySize))
                    _arrayType = spec;
            }
        }

        if (_default == null)
            throw new InvalidOperationException("No default type was specified");
    }

    public ITypeSpec? Get(DataType type) => _index[(int)type];
    public ITypeSpec<T>? Get<T>() => (ITypeSpec<T>)_index[(int)Type.GetTypeCode(typeof(T))];

    public ITypeSpec GetRequired<T>()
    {
        ITypeSpec<T>? res = Get<T>();

        if (res == null)
            throw new InvalidOperationException("No type spec was found for " + typeof(T).Name);

        return res;
    }

    public ITypeSpec GetArraySizeType() => _arrayType;
}