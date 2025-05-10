using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public sealed class TypeMap
{
    private readonly ITypeSpec?[] _index = new ITypeSpec?[19];
    private readonly ITypeSpec _default;

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
            }
        }

        if (_default == null)
            throw new InvalidOperationException("No default type was specified");
    }

    public ITypeSpec<T> Get<T>()
    {
        ITypeSpec<T>? res = (ITypeSpec<T>?)_index[(int)Type.GetTypeCode(typeof(T))];

        if (res == null)
            throw new InvalidOperationException("No type spec was found for " + typeof(T).Name);

        return res;
    }
}