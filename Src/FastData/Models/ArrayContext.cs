using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Models;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
public class ArrayContext(object[] data) : DefaultContext(data);