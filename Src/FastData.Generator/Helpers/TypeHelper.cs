namespace Genbox.FastData.Generator.Helpers;

public static class TypeHelper
{
    public static IEnumerable<Type> GetCustomTypes(Type type)
    {
        HashSet<Type> uniq = new HashSet<Type>();
        Queue<Type> queue = new Queue<Type>();

        queue.Enqueue(type);

        while (queue.Count > 0)
        {
            Type t = queue.Dequeue();

            if (Type.GetTypeCode(t) != TypeCode.Object)
                continue;

            if (t.IsArray)
            {
                Type elementType = t.GetElementType()!;
                if (uniq.Add(elementType))
                    queue.Enqueue(elementType);

                continue;
            }

            yield return t;
        }
    }
}