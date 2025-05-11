using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record FloatProperties<T>(T MinValue, T MaxValue) : IHasMinMax<T>;