using System.Linq.Expressions;

namespace Genbox.FastData.Internal.Misc;

internal delegate Expression Mixer(Expression hash, Expression readFunc);